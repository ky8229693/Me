using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Bootstrap;

public static class ChineseDemoSeedCatalog
{
    public static readonly Guid ProjectId = Guid.Parse("e0000000-0000-4000-8000-000000000001");
    public const string ProjectName = "WEB_Demo 開發專案";
    public const string ProjectDescription = "以本專案實際開發歷程展示專案規劃、議題追蹤與協作流程。";

    public static readonly IReadOnlyCollection<DemoLabelSeed> Labels =
    [
        new(Guid.Parse("e1000000-0000-4000-8000-000000000001"), "後端", "#2563EB"),
        new(Guid.Parse("e1000000-0000-4000-8000-000000000002"), "前端", "#7C3AED"),
        new(Guid.Parse("e1000000-0000-4000-8000-000000000003"), "資安", "#DC2626"),
        new(Guid.Parse("e1000000-0000-4000-8000-000000000004"), "維運", "#EA580C"),
        new(Guid.Parse("e1000000-0000-4000-8000-000000000005"), "測試", "#16A34A"),
        new(Guid.Parse("e1000000-0000-4000-8000-000000000006"), "文件", "#0891B2")
    ];

    public static readonly IReadOnlyCollection<DemoIssueSeed> Issues =
    [
        new(Guid.Parse("e2000000-0000-4000-8000-000000000001"), "建立 API 基礎架構", "完成 Minimal API 模組、集中錯誤處理、CORS 與健康檢查。", IssueType.Feature, IssuePriority.High, IssueStatus.Done, -40, -25, ["後端"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000002"), "實作驗證與權限控管", "整合 Identity、JWT rotation、session allowlist 與 permission policies。", IssueType.Feature, IssuePriority.Critical, IssueStatus.Done, -32, -18, ["後端", "資安"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000003"), "完成專案與議題管理", "提供專案成員、議題流程、留言與不可變更的歷程紀錄。", IssueType.Feature, IssuePriority.High, IssueStatus.Done, -22, -10, ["後端", "前端"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000004"), "改善 Kanban 看板與專案標籤", "完成可分享網址的清單／看板切換與專案範圍標籤。", IssueType.Feature, IssuePriority.High, IssueStatus.QA, -8, 2, ["前端", "後端", "測試"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000005"), "建置個人工作台", "彙整指派工作、逾期議題、狀態統計與最近活動。", IssueType.Feature, IssuePriority.High, IssueStatus.InProgress, -6, -1, ["前端", "後端"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000006"), "導入繁體中文與英文介面", "預設使用繁體中文，支援英文切換並保存使用者選擇。", IssueType.ChangeRequest, IssuePriority.Medium, IssueStatus.Draft, -1, 5, ["前端", "文件"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000007"), "加入結構化應用程式記錄", "提供可搜尋的 request 與 business context，且不得暴露機密。", IssueType.ChangeRequest, IssuePriority.Medium, IssueStatus.Approved, 0, 14, ["後端", "維運"]),
        new(Guid.Parse("e2000000-0000-4000-8000-000000000008"), "驗證正式環境容器", "在 CI 驗證 gateway、API image 與本機正式環境操作流程。", IssueType.Bug, IssuePriority.High, IssueStatus.QA, -4, 3, ["維運", "測試"])
    ];

    public static readonly Guid CommentId = Guid.Parse("e3000000-0000-4000-8000-000000000001");
    public static readonly Guid HistoryId = Guid.Parse("e4000000-0000-4000-8000-000000000001");
}
