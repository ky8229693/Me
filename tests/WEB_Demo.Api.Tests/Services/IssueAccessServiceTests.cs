using FluentAssertions;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Tests.Services;

public sealed class IssueAccessServiceTests
{
    [Fact]
    public void Member_CanEditOnlyOwnDraft()
    {
        var member = Membership(ProjectRole.Member, "member");
        IssueAccessService.CanEdit(member, ProjectStatus.Active, Issue("member", IssueStatus.Draft)).Should().BeTrue();
        IssueAccessService.CanEdit(member, ProjectStatus.Active, Issue("other", IssueStatus.Draft)).Should().BeFalse();
        IssueAccessService.CanEdit(member, ProjectStatus.Active, Issue("member", IssueStatus.Approved)).Should().BeFalse();
    }

    [Fact]
    public void AssignedMember_CanAdvanceActiveWorkButCannotApproveOrComplete()
    {
        var member = Membership(ProjectRole.Member, "member");
        IssueAccessService.AllowedTransitions(member, ProjectStatus.Active, Issue("other", IssueStatus.Approved, "member"))
            .Should().Equal(IssueStatus.InProgress);
        IssueAccessService.AllowedTransitions(member, ProjectStatus.Active, Issue("other", IssueStatus.QA, "member"))
            .Should().Equal(IssueStatus.InProgress);
        IssueAccessService.AllowedTransitions(member, ProjectStatus.Active, Issue("member", IssueStatus.Draft, "member"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ArchivedProject_DisablesAllMutations()
    {
        var owner = Membership(ProjectRole.Owner, "owner");
        var issue = Issue("owner", IssueStatus.Draft);
        IssueAccessService.CanCreate(owner, ProjectStatus.Archived).Should().BeFalse();
        IssueAccessService.CanEdit(owner, ProjectStatus.Archived, issue).Should().BeFalse();
        IssueAccessService.CanAssign(owner, ProjectStatus.Archived).Should().BeFalse();
        IssueAccessService.CanComment(owner, ProjectStatus.Archived).Should().BeFalse();
        IssueAccessService.AllowedTransitions(owner, ProjectStatus.Archived, issue).Should().BeEmpty();
    }

    private static ProjectMembership Membership(ProjectRole role, string userId) => new()
    {
        ProjectId = Guid.NewGuid(), UserId = userId, Role = role
    };

    private static Issue Issue(string creator, IssueStatus status, string? assignee = null) => new()
    {
        ProjectId = Guid.NewGuid(), Title = "Issue", Type = IssueType.Bug,
        Priority = IssuePriority.High, CreatorUserId = creator, AssigneeUserId = assignee, Status = status
    };
}
