namespace WEB_Demo.Api.Data;

public sealed class IssueLabel
{
    public Guid IssueId { get; set; }
    public Guid LabelId { get; set; }
    public Issue Issue { get; set; } = null!;
    public ProjectLabel Label { get; set; } = null!;
}
