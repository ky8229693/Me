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
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueHistory> IssueHistory => Set<IssueHistory>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<ProjectLabel> ProjectLabels => Set<ProjectLabel>();
    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();
    public DbSet<SpecVectorDocument> SpecVectorDocuments => Set<SpecVectorDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            builder.HasPostgresExtension("vector");
            builder.Entity<SpecVectorDocument>(entity =>
            {
                entity.ToTable("SpecVectorDocuments");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ChunkId).HasMaxLength(600).IsRequired();
                entity.Property(item => item.Capability).HasMaxLength(200).IsRequired();
                entity.Property(item => item.SourcePath).HasMaxLength(1000).IsRequired();
                entity.Property(item => item.Content).HasMaxLength(12000).IsRequired();
                entity.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
                entity.Property(item => item.EmbeddingModel).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Embedding).HasColumnType("vector(1024)").IsRequired();
                entity.HasIndex(item => new { item.ChunkId, item.EmbeddingModel }).IsUnique();
                entity.HasIndex(item => item.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
            });
        }
        else builder.Ignore<SpecVectorDocument>();

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

        builder.Entity<Issue>(entity =>
        {
            entity.ToTable("Issues");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(10000);
            entity.Property(item => item.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.CreatorUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.AssigneeUserId).HasMaxLength(450);
            entity.HasIndex(item => new { item.ProjectId, item.UpdatedAt });
            entity.HasIndex(item => new { item.ProjectId, item.Status });
            entity.HasIndex(item => new { item.ProjectId, item.Type });
            entity.HasIndex(item => new { item.ProjectId, item.Priority });
            entity.HasIndex(item => new { item.ProjectId, item.AssigneeUserId });
            entity.HasIndex(item => new { item.AssigneeUserId, item.Status, item.DueDate });
            entity.HasOne(item => item.Project).WithMany(project => project.Issues)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.CreatorUser).WithMany()
                .HasForeignKey(item => item.CreatorUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.AssigneeUser).WithMany()
                .HasForeignKey(item => item.AssigneeUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<IssueHistory>(entity =>
        {
            entity.ToTable("IssueHistory");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.ChangeType).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.FieldName).HasMaxLength(50);
            entity.Property(item => item.PreviousValue).HasMaxLength(10000);
            entity.Property(item => item.NewValue).HasMaxLength(10000);
            entity.HasIndex(item => new { item.IssueId, item.CreatedAt });
            entity.HasIndex(item => new { item.ProjectId, item.CreatedAt });
            entity.HasOne(item => item.Project).WithMany(project => project.IssueHistory)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Issue).WithMany(issue => issue.History)
                .HasForeignKey(item => item.IssueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ActorUser).WithMany()
                .HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IssueComment>(entity =>
        {
            entity.ToTable("IssueComments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.AuthorUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Content).HasMaxLength(5000).IsRequired();
            entity.HasIndex(item => new { item.IssueId, item.CreatedAt });
            entity.HasIndex(item => new { item.ProjectId, item.CreatedAt });
            entity.HasOne(item => item.Project).WithMany(project => project.IssueComments)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Issue).WithMany(issue => issue.Comments)
                .HasForeignKey(item => item.IssueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.AuthorUser).WithMany()
                .HasForeignKey(item => item.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectLabel>(entity =>
        {
            entity.ToTable("ProjectLabels");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(40).IsRequired();
            entity.Property(item => item.NormalizedName).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Color).HasMaxLength(7).IsRequired();
            entity.HasIndex(item => new { item.ProjectId, item.NormalizedName }).IsUnique();
            entity.HasOne(item => item.Project).WithMany(project => project.Labels)
                .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IssueLabel>(entity =>
        {
            entity.ToTable("IssueLabels");
            entity.HasKey(item => new { item.IssueId, item.LabelId });
            entity.HasIndex(item => item.LabelId);
            entity.HasOne(item => item.Issue).WithMany(issue => issue.IssueLabels)
                .HasForeignKey(item => item.IssueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Label).WithMany(label => label.IssueLabels)
                .HasForeignKey(item => item.LabelId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
