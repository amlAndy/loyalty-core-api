using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Infrastructure.Admin.Users;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Infrastructure.Tests.Admin.Users;

public sealed class AdminUserRepositoryTests
{
    [Fact]
    public async Task GetFormOptionsAsync_returns_statuses_and_sorted_role_projections()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.GetFormOptionsAsync();

        Assert.Equal(["Active", "Inactive", "Locked", "PendingInvite"], result.Statuses);
        Assert.Collection(
            result.Roles,
            role =>
            {
                Assert.Equal("admin", role.Id);
                Assert.Equal("Admin", role.Name);
                Assert.Null(role.ParentRoleId);
                Assert.True(role.IsRootRole);
                Assert.True(role.IsSystemRole);
            },
            role =>
            {
                Assert.Equal("member", role.Id);
                Assert.Equal("Member", role.Name);
                Assert.Null(role.ParentRoleId);
                Assert.True(role.IsRootRole);
                Assert.True(role.IsSystemRole);
            },
            role =>
            {
                Assert.Equal("supplier", role.Id);
                Assert.Equal("Supplier", role.Name);
                Assert.Null(role.ParentRoleId);
                Assert.True(role.IsRootRole);
                Assert.True(role.IsSystemRole);
            });
    }

    [Fact]
    public async Task GetByIdAsync_returns_user_detail_when_found()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.GetByIdAsync("admin-user");

        Assert.NotNull(result);
        Assert.Equal("admin-user", result!.Id);
        Assert.Equal("Admin User", result.DisplayName);
        Assert.Equal("admin@example.com", result.Email);
        Assert.Equal("Active", result.Status);
        Assert.Equal(["admin"], result.RoleIds);
        Assert.Equal(["Admin"], result.Roles);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.GetByIdAsync("missing-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_persists_a_user()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.CreateAsync(new AdminUserUpsertModel
        {
            DisplayName = "Created User",
            Email = "created@example.com",
            Status = "PendingInvite",
            RoleIds = ["member"]
        });

        Assert.Equal("Created User", result.DisplayName);
        Assert.Equal("created@example.com", result.Email);
        Assert.Equal("PendingInvite", result.Status);
        Assert.Equal(["member"], result.RoleIds);
        Assert.Equal(["Member"], result.Roles);

        var persistedUser = await dbContext.Users.SingleAsync(user => user.Email == "created@example.com");
        Assert.Equal("Created User", persistedUser.DisplayName);
        Assert.Equal("PendingInvite", persistedUser.Status);
    }

    [Fact]
    public async Task DeleteAsync_deletes_existing_user_and_removes_it_from_queries()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var deleted = await repository.DeleteAsync("member-user");

        Assert.True(deleted);
        Assert.Null(await repository.GetByIdAsync("member-user"));

        var searchResult = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            PageNumber = 1,
            PageSize = 10
        });

        Assert.DoesNotContain(searchResult.Items, item => item.Id == "member-user");
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_user_is_missing()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var deleted = await repository.DeleteAsync("missing-user");

        Assert.False(deleted);
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.UpdateAsync("member-user", new AdminUserUpsertModel
        {
            DisplayName = "Updated Member",
            Email = "updated-member@example.com",
            Status = "Locked",
            RoleIds = ["supplier"]
        });

        Assert.NotNull(result);
        Assert.Equal("Updated Member", result!.DisplayName);
        Assert.Equal("updated-member@example.com", result.Email);
        Assert.Equal("Locked", result.Status);
        Assert.Equal(["supplier"], result.RoleIds);
        Assert.Equal(["Supplier"], result.Roles);
    }

    [Fact]
    public async Task SearchUsersAsync_filters_by_query_status_and_role_id_case_insensitively()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            Query = "ADMIN",
            RoleId = "admin",
            Status = "active",
            PageNumber = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal("admin-user", item.Id);
        Assert.Equal("admin@example.com", item.Email);
        Assert.Equal(["admin"], item.RoleIds);
        Assert.Equal(["Admin"], item.Roles);
    }

    [Fact]
    public async Task SearchUsersAsync_returns_paged_and_sorted_matches_without_filters()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            PageNumber = 1,
            PageSize = 2
        });

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Length);
        Assert.Collection(
            result.Items,
            item => Assert.Equal("admin@example.com", item.Email),
            item => Assert.Equal("member@example.com", item.Email));
    }

    [Fact]
    public async Task SearchUsersAsync_applies_paging_after_counting_filtered_matches()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            Status = "Active",
            PageNumber = 2,
            PageSize = 1
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal("member@example.com", item.Email);
    }

    [Fact]
    public async Task SearchUsersAsync_applies_query_filter()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            Query = "supplier",
            PageNumber = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("supplier@example.com", item.Email);
    }

    [Fact]
    public async Task SearchUsersAsync_applies_role_filter()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            RoleId = "member",
            PageNumber = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("member@example.com", item.Email);
    }

    [Fact]
    public async Task SearchUsersAsync_applies_status_filter()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        var repository = CreateRepository(dbContext);

        var result = await repository.SearchUsersAsync(new AdminUserSearchCriteria
        {
            Status = "Inactive",
            PageNumber = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("supplier@example.com", item.Email);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AdminUserRepository CreateRepository(ApplicationDbContext dbContext)
    {
        var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(dbContext);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

        return new AdminUserRepository(dbContext, userManager);
    }

    private static void SeedUsers(ApplicationDbContext dbContext)
    {
        var memberRole = new ApplicationRole { Id = "member", Name = "Member", NormalizedName = "MEMBER", IsRootRole = true, IsSystemRole = true };
        var supplierRole = new ApplicationRole { Id = "supplier", Name = "Supplier", NormalizedName = "SUPPLIER", IsRootRole = true, IsSystemRole = true };
        var adminRole = new ApplicationRole { Id = "admin", Name = "Admin", NormalizedName = "ADMIN", IsRootRole = true, IsSystemRole = true };

        var memberUser = new ApplicationUser
        {
            Id = "member-user",
            UserName = "member@example.com",
            NormalizedUserName = "MEMBER@EXAMPLE.COM",
            Email = "member@example.com",
            NormalizedEmail = "MEMBER@EXAMPLE.COM",
            DisplayName = "Member User",
            Status = "Active"
        };
        var supplierUser = new ApplicationUser
        {
            Id = "supplier-user",
            UserName = "supplier@example.com",
            NormalizedUserName = "SUPPLIER@EXAMPLE.COM",
            Email = "supplier@example.com",
            NormalizedEmail = "SUPPLIER@EXAMPLE.COM",
            DisplayName = "Supplier User",
            Status = "Inactive"
        };
        var adminUser = new ApplicationUser
        {
            Id = "admin-user",
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            DisplayName = "Admin User",
            Status = "Active"
        };

        dbContext.Roles.AddRange(memberRole, supplierRole, adminRole);
        dbContext.Users.AddRange(memberUser, supplierUser, adminUser);
        dbContext.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = memberUser.Id, RoleId = memberRole.Id },
            new IdentityUserRole<string> { UserId = supplierUser.Id, RoleId = supplierRole.Id },
            new IdentityUserRole<string> { UserId = adminUser.Id, RoleId = adminRole.Id });
        dbContext.SaveChanges();
    }
}
