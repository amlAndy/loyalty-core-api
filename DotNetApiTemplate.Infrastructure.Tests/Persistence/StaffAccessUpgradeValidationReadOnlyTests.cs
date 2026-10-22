using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using DotNetApiTemplate.Infrastructure.Persistence.Migrations;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Infrastructure.Tests.Persistence;

public sealed class StaffAccessUpgradeValidationReadOnlyTests
{
    [Fact]
    public void Up_AddsAccessStatusColumn_WithSuspendedDefault()
    {
        var operations = BuildUpOperations();

        var addColumn = Assert.Single(operations.OfType<AddColumnOperation>());
        Assert.Equal("BusinessStaffAccesses", addColumn.Table);
        Assert.Equal("AccessStatus", addColumn.Name);
        Assert.Equal("nvarchar(50)", addColumn.ColumnType);
        Assert.Equal(50, addColumn.MaxLength);
        Assert.False(addColumn.IsNullable);
        Assert.Equal("Suspended", addColumn.DefaultValue);
    }

    [Fact]
    public void Up_BackfillSql_OnlySetsActiveForLinkedRowsWithActivePinStatus()
    {
        var operations = BuildUpOperations();

        var sql = Assert.Single(operations.OfType<SqlOperation>()).Sql;
        Assert.Contains("SET [AccessStatus] = N'Suspended'", sql, StringComparison.Ordinal);
        Assert.Contains("SET [AccessStatus] = N'Active'", sql, StringComparison.Ordinal);
        Assert.Contains("UPPER(ISNULL([PinStatus], N'')) = N'ACTIVE'", sql, StringComparison.Ordinal);
        Assert.Contains("[UserId] IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("LTRIM(RTRIM([UserId])) <> N''", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Up_CreatesBusinessProfileAccessStatusIndex()
    {
        var operations = BuildUpOperations();

        var index = Assert.Single(operations.OfType<CreateIndexOperation>());
        Assert.Equal("IX_BusinessStaffAccesses_BusinessProfileId_AccessStatus", index.Name);
        Assert.Equal("BusinessStaffAccesses", index.Table);
        Assert.Equal(new[] { "BusinessProfileId", "AccessStatus" }, index.Columns);
    }

    private static IReadOnlyList<MigrationOperation> BuildUpOperations()
    {
        var migration = new AddBusinessStaffAccessStatus();
        var migrationBuilder = new MigrationBuilder("SqlServer");

        var upMethod = typeof(AddBusinessStaffAccessStatus).GetMethod(
            "Up",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        upMethod!.Invoke(migration, new object[] { migrationBuilder });

        return migrationBuilder.Operations;
    }
}

public sealed class StaffAccessUpgradeValidationReadOnlyLocalDbIntegrationTests
{
    private const string LocalDbName = "LOCALDB_STAFFACCESS_UPGRADE_VALIDATION_20261022";
    private const string LocalDbConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=" + LocalDbName + ";Trusted_Connection=True;TrustServerCertificate=True;";
    private static readonly Guid LegacyBusinessA = Guid.Parse("22222222-2222-2222-2222-222222222281");

    [Fact]
    [Trait("Category", "OptInLocalDbUpgradeValidation")]
    public async Task IdentityUserAuthRepository_GetUserByAccessTokenAsync_RejectsAmbiguousLegacyStaffBusinessSession()
    {
        await using var dbContext = CreateLocalDbContext();
        AssertSqlServerLocalDb(dbContext);

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var roleId = await EnsureRoleAssignedAsync(dbContext, "staff-ambiguous", ApplicationRoleNames.BusinessStaff);
        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == "staff-ambiguous");

        var jwt = CreateJwtTokenService();
        var (accessToken, _) = jwt.GenerateAccessToken(user, [ApplicationRoleNames.BusinessStaff], Guid.NewGuid(), roleId);
        var repository = new IdentityUserAuthRepository(dbContext, CreateUserManager(dbContext), jwt);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.GetUserByAccessTokenAsync(accessToken, CancellationToken.None));

        Assert.Contains("Ambiguous business staff context", exception.Message, StringComparison.OrdinalIgnoreCase);

        await tx.RollbackAsync();
    }

    [Fact]
    [Trait("Category", "OptInLocalDbUpgradeValidation")]
    public async Task BusinessAccessAuthorizationService_AuthorizeAsync_RejectsDuplicateActiveAssignmentsWithinOneBusiness()
    {
        await using var dbContext = CreateLocalDbContext();
        AssertSqlServerLocalDb(dbContext);

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var roleId = await EnsureRoleAssignedAsync(dbContext, "staff-active", ApplicationRoleNames.BusinessStaff);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-44444444a901"),
            BusinessProfileId = LegacyBusinessA,
            UserId = "staff-active",
            DisplayName = "Staff Active Duplicate",
            BusinessAccessRole = "Staff",
            AccessStatus = "Active",
            PinHash = "hash-dup",
            PinStatus = "Active",
            LastActivitySummary = "Isolated duplicate fixture",
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new BusinessAccessAuthorizationService(dbContext);

        var result = await service.AuthorizeAsync(
            userId: "staff-active",
            activeRoleId: roleId,
            businessId: LegacyBusinessA,
            operation: BusinessAccessOperation.RedeemPoints,
            cancellationToken: CancellationToken.None);

        Assert.Equal(BusinessAccessAuthorizationResult.Forbidden, result);

        await tx.RollbackAsync();
    }

    [Fact]
    [Trait("Category", "OptInLocalDbUpgradeValidation")]
    public async Task LoveBrackleyService_RedeemStaffResolution_RejectsDuplicateActiveAssignments_WithoutSubmittingTransaction()
    {
        await using var dbContext = CreateLocalDbContext();
        AssertSqlServerLocalDb(dbContext);

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var roleId = await EnsureRoleAssignedAsync(dbContext, "staff-active", ApplicationRoleNames.BusinessStaff);
        _ = roleId;

        var baselineTxCount = await dbContext.LoyaltyTransactions.CountAsync();

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-44444444a902"),
            BusinessProfileId = LegacyBusinessA,
            UserId = "staff-active",
            DisplayName = "Staff Active Duplicate Redeem",
            BusinessAccessRole = "Staff",
            AccessStatus = "Active",
            PinHash = "hash-dup-redeem",
            PinStatus = "Active",
            LastActivitySummary = "Isolated duplicate fixture for redeem",
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            InvokeResolveRedeemStaffAccessAsync(service, LegacyBusinessA, "staff-active", roleId, CancellationToken.None));

        Assert.Contains(exception.Errors, error =>
            string.Equals(error.PropertyName, "staffAccess", StringComparison.OrdinalIgnoreCase)
            && error.ErrorMessage.Contains("Ambiguous staff access context", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(baselineTxCount, await dbContext.LoyaltyTransactions.CountAsync());

        await tx.RollbackAsync();
    }

    private static ApplicationDbContext CreateLocalDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(LocalDbConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void AssertSqlServerLocalDb(ApplicationDbContext dbContext)
    {
        Assert.Contains("SqlServer", dbContext.Database.ProviderName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LocalDbName, dbContext.Database.GetDbConnection().Database);
    }

    private static async Task<string> EnsureRoleAssignedAsync(ApplicationDbContext dbContext, string userId, string roleName)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(candidate => candidate.Name == roleName);
        if (role is null)
        {
            role = new ApplicationRole
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = roleName,
                IsRootRole = false,
                IsSystemRole = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync();
        }

        var alreadyAssigned = await dbContext.UserRoles.AnyAsync(candidate => candidate.UserId == userId && candidate.RoleId == role.Id);
        if (!alreadyAssigned)
        {
            dbContext.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = role.Id
            });
            await dbContext.SaveChangesAsync();
        }

        return role.Id;
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(dbContext);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var lookupNormalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();

        return new UserManager<ApplicationUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            lookupNormalizer,
            errorDescriber,
            services,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:Secret"] = "12345678901234567890123456789012"
            })
            .Build();

        return new JwtTokenService(configuration);
    }

    private static async Task<BusinessStaffAccessEntity?> InvokeResolveRedeemStaffAccessAsync(
        LoveBrackleyService service,
        Guid businessId,
        string actorUserId,
        string activeRoleId,
        CancellationToken cancellationToken)
    {
        var method = typeof(LoveBrackleyService).GetMethod(
            "ResolveRedeemStaffAccessAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task<BusinessStaffAccessEntity?>)method!.Invoke(service, [businessId, actorUserId, activeRoleId, cancellationToken])!;
        return await task;
    }
}
