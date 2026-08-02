using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WEB_Demo.Api.Bootstrap;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class DevelopmentDemoSeedServiceTests
{
    [Fact]
    public async Task Seed_FreshDatabase_CreatesRepresentativeDashboardAndKanbanData()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var (service, user, db) = await CreateOwnerAsync(scope.ServiceProvider);

        var result = await service.SeedAsync(user.Email!, true);
        var dashboard = await scope.ServiceProvider.GetRequiredService<DashboardService>().GetAsync(user.Id);

        result.Succeeded.Should().BeTrue();
        (await db.Projects.CountAsync(item => item.Id == DemoSeedCatalog.ProjectId)).Should().Be(1);
        (await db.Projects.CountAsync(item => item.Id == ChineseDemoSeedCatalog.ProjectId && item.Name == ChineseDemoSeedCatalog.ProjectName)).Should().Be(1);
        (await db.ProjectLabels.CountAsync(item => item.ProjectId == DemoSeedCatalog.ProjectId)).Should().Be(DemoSeedCatalog.Labels.Count);
        (await db.ProjectLabels.CountAsync(item => item.ProjectId == ChineseDemoSeedCatalog.ProjectId)).Should().Be(ChineseDemoSeedCatalog.Labels.Count);
        (await db.Issues.Where(item => item.ProjectId == DemoSeedCatalog.ProjectId).Select(item => item.Status).Distinct().CountAsync()).Should().Be(5);
        (await db.Issues.Where(item => item.ProjectId == ChineseDemoSeedCatalog.ProjectId).Select(item => item.Status).Distinct().CountAsync()).Should().Be(5);
        (await db.Issues.AnyAsync(item => item.ProjectId == ChineseDemoSeedCatalog.ProjectId && item.Title.Contains("繁體中文"))).Should().BeTrue();
        dashboard.AssignedCount.Should().BeGreaterThan(0);
        dashboard.OverdueCount.Should().BeGreaterThan(0);
        dashboard.RecentActivity.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Seed_Twice_IsIdempotentAndPreservesUserCreatedData()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var (service, user, db) = await CreateOwnerAsync(scope.ServiceProvider);
        (await service.SeedAsync(user.Email!, true)).Succeeded.Should().BeTrue();
        var custom = new Issue { ProjectId = DemoSeedCatalog.ProjectId, Title = "Custom local work",
            Type = IssueType.Bug, Priority = IssuePriority.Low, Status = IssueStatus.Draft,
            CreatorUserId = user.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.Issues.Add(custom);
        await db.SaveChangesAsync();

        var second = await service.SeedAsync(user.Email!, true);

        second.Succeeded.Should().BeTrue();
        (await db.Projects.CountAsync(item => item.Id == DemoSeedCatalog.ProjectId)).Should().Be(1);
        (await db.Projects.CountAsync(item => item.Id == ChineseDemoSeedCatalog.ProjectId)).Should().Be(1);
        (await db.Projects.SingleAsync(item => item.Id == DemoSeedCatalog.ProjectId)).Name.Should().Be(DemoSeedCatalog.ProjectName);
        (await db.Issues.CountAsync(item => DemoSeedCatalog.Issues.Select(seed => seed.Id).Contains(item.Id))).Should().Be(DemoSeedCatalog.Issues.Count);
        (await db.Issues.CountAsync(item => ChineseDemoSeedCatalog.Issues.Select(seed => seed.Id).Contains(item.Id))).Should().Be(ChineseDemoSeedCatalog.Issues.Count);
        (await db.Issues.AnyAsync(item => item.Id == custom.Id)).Should().BeTrue();
        (await db.IssueComments.CountAsync(item => item.Id == DemoSeedCatalog.CommentId)).Should().Be(1);
        (await db.IssueComments.CountAsync(item => item.Id == ChineseDemoSeedCatalog.CommentId)).Should().Be(1);
    }

    [Fact]
    public async Task Seed_MissingOrDisabledOwner_AndNonDevelopment_WriteNothing()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DevelopmentDemoSeedService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await service.SeedAsync("missing@example.com", true)).Succeeded.Should().BeFalse();
        var (_, user, _) = await CreateOwnerAsync(scope.ServiceProvider);
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        (await manager.UpdateAsync(user)).Succeeded.Should().BeTrue();
        (await service.SeedAsync(user.Email!, true)).Succeeded.Should().BeFalse();
        user.LockoutEnd = null;
        (await manager.UpdateAsync(user)).Succeeded.Should().BeTrue();
        (await service.SeedAsync(user.Email!, false)).Succeeded.Should().BeFalse();
        (await db.Projects.AnyAsync(item => item.Id == DemoSeedCatalog.ProjectId)).Should().BeFalse();
        (await db.Projects.AnyAsync(item => item.Id == ChineseDemoSeedCatalog.ProjectId)).Should().BeFalse();
    }

    private static async Task<(DevelopmentDemoSeedService Service, IdentityUser User, AppDbContext Db)> CreateOwnerAsync(IServiceProvider services)
    {
        var manager = services.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"demo-owner-{Guid.NewGuid():N}@example.com";
        var user = new IdentityUser { UserName = email, Email = email };
        (await manager.CreateAsync(user, "123456")).Succeeded.Should().BeTrue();
        return (services.GetRequiredService<DevelopmentDemoSeedService>(), user,
            services.GetRequiredService<AppDbContext>());
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"demo-seed-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }
}
