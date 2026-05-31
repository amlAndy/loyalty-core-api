using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditChangeDetailEntity> AuditChangeDetails => Set<AuditChangeDetailEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(user => user.Status)
                .IsRequired()
                .HasMaxLength(50);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(role => role.Description)
                .IsRequired()
                .HasDefaultValue(string.Empty);

            entity.Property(role => role.CreatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(role => role.UpdatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(role => role.IsRootRole)
                .HasDefaultValue(false);

            entity.Property(role => role.IsSystemRole)
                .HasDefaultValue(false);

            entity.HasIndex(role => role.ParentRoleId);

            entity.HasOne(role => role.ParentRole)
                .WithMany(role => role.ChildRoles)
                .HasForeignKey(role => role.ParentRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.ActingUser)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(entry => entry.ActionLabel)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(entry => entry.TargetId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(entry => entry.TargetName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Target)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Summary)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasIndex(entry => entry.OccurredAtUtc);
            entity.HasIndex(entry => new { entry.Action, entry.OccurredAtUtc });
            entity.HasIndex(entry => new { entry.EntityType, entry.OccurredAtUtc });
            entity.HasIndex(entry => entry.ActingUser);
            entity.HasIndex(entry => entry.TargetId);

            entity.HasMany(entry => entry.Details)
                .WithOne(detail => detail.AuditEntry)
                .HasForeignKey(detail => detail.AuditEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditChangeDetailEntity>(entity =>
        {
            entity.ToTable("AuditEntryDetails");

            entity.HasKey(detail => detail.Id);

            entity.Property(detail => detail.Field)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(detail => detail.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(detail => new { detail.AuditEntryId, detail.Sequence });
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(refreshToken => refreshToken.Id);

            entity.Property(refreshToken => refreshToken.Token)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(refreshToken => refreshToken.ActiveRoleId)
                .HasMaxLength(450);

            entity.Property(refreshToken => refreshToken.CreatedByIp)
                .HasMaxLength(45);

            entity.Property(refreshToken => refreshToken.RevokedByIp)
                .HasMaxLength(45);

            entity.Property(refreshToken => refreshToken.ReplacedByToken)
                .HasMaxLength(200);

            entity.HasIndex(refreshToken => refreshToken.Token)
                .IsUnique();

            entity.HasIndex(refreshToken => refreshToken.UserId);

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(passwordResetToken => passwordResetToken.Id);

            entity.Property(passwordResetToken => passwordResetToken.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(passwordResetToken => passwordResetToken.TokenHash)
                .IsUnique();

            entity.HasIndex(passwordResetToken => passwordResetToken.UserId);
            entity.HasIndex(passwordResetToken => passwordResetToken.ExpiresAtUtc);

            entity.HasOne(passwordResetToken => passwordResetToken.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(passwordResetToken => passwordResetToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
