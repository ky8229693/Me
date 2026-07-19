using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WEB_Demo.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.FamilyId).HasMaxLength(32).IsRequired();
            entity.Property(token => token.UserId).HasMaxLength(450).IsRequired();
            entity.Property(token => token.RevocationReason).HasMaxLength(128);
            entity.Property(token => token.ConcurrencyStamp).IsConcurrencyToken();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.FamilyId);
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.ExpiresAt);
            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Key).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Name).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(300).IsRequired();
            entity.Property(permission => permission.Category).HasMaxLength(100).IsRequired();
            entity.HasIndex(permission => permission.Key).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(mapping => new { mapping.RoleId, mapping.PermissionId });
            entity.HasOne(mapping => mapping.Role).WithMany().HasForeignKey(mapping => mapping.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mapping => mapping.Permission).WithMany(permission => permission.RolePermissions)
                .HasForeignKey(mapping => mapping.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
