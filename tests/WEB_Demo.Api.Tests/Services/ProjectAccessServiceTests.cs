using FluentAssertions;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Tests.Services;

public sealed class ProjectAccessServiceTests
{
    [Theory]
    [InlineData(ProjectRole.Owner, ProjectAction.Read, true)]
    [InlineData(ProjectRole.Manager, ProjectAction.Update, true)]
    [InlineData(ProjectRole.Member, ProjectAction.Update, false)]
    [InlineData(ProjectRole.Viewer, ProjectAction.ManageMembers, false)]
    [InlineData(ProjectRole.Owner, ProjectAction.Archive, true)]
    [InlineData(ProjectRole.Manager, ProjectAction.Archive, false)]
    public void ActiveProject_UsesFixedRoleCapabilities(ProjectRole role, ProjectAction action, bool expected)
    {
        var membership = Membership(role);

        ProjectAccessService.IsAllowed(membership, ProjectStatus.Active, action).Should().Be(expected);
    }

    [Fact]
    public void ArchivedProject_IsReadOnlyExceptOwnerRestore()
    {
        ProjectAccessService.IsAllowed(Membership(ProjectRole.Owner), ProjectStatus.Archived, ProjectAction.Read).Should().BeTrue();
        ProjectAccessService.IsAllowed(Membership(ProjectRole.Manager), ProjectStatus.Archived, ProjectAction.Update).Should().BeFalse();
        ProjectAccessService.IsAllowed(Membership(ProjectRole.Owner), ProjectStatus.Archived, ProjectAction.Restore).Should().BeTrue();
        ProjectAccessService.IsAllowed(Membership(ProjectRole.Manager), ProjectStatus.Archived, ProjectAction.Restore).Should().BeFalse();
    }

    private static ProjectMembership Membership(ProjectRole role) => new()
    {
        ProjectId = Guid.NewGuid(),
        UserId = "user",
        Role = role
    };
}

