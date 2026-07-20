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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();
    public DbSet<ProjectAuditEvent> ProjectAuditEvents => Set<ProjectAuditEvent>();

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

        builder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(100).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000);
            entity.Property(project => project.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(project => project.Status);
            entity.HasIndex(project => project.Name);
        });

        builder.Entity<ProjectMembership>(entity =>
        {
            entity.ToTable("ProjectMemberships");
            entity.HasKey(item => new { item.ProjectId, item.UserId });
            entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => new { item.ProjectId, item.Role });
            entity.HasOne(item => item.Project).WithMany(project => project.Memberships)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User).WithMany()
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectAuditEvent>(entity =>
        {
            entity.ToTable("ProjectAuditEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Action).HasMaxLength(100).IsRequired();
            entity.Property(item => item.TargetType).HasMaxLength(50).IsRequired();
            entity.Property(item => item.TargetId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.PreviousValue).HasMaxLength(1000);
            entity.Property(item => item.NewValue).HasMaxLength(1000);
            entity.HasIndex(item => new { item.ProjectId, item.CreatedAt });
            entity.HasOne(item => item.Project).WithMany(project => project.AuditEvents)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ActorUser).WithMany()
                .HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
