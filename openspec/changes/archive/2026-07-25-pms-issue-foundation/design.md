## Context

`pms-project-foundation` 已建立 Project、membership、固定 Project Role、封存唯讀與 Audit 邊界。下一步需要在該邊界內提供可實際使用的工作追蹤，並累積日後文件／API 關聯、報表與 AI 可引用的結構化 Issue 歷程。

系統維持單一公司部署、ASP.NET Core Minimal APIs、PostgreSQL、EF Core、Vue 3、PrimeVue 及既有 ApiResponse 合約。所有 Issue 資料必須直接關聯 ProjectId，且 System Admin 不得繞過 Project membership。

## Goals / Non-Goals

**Goals:**

- 支援 Feature、Bug、Change Request 的基本建立、查詢、更新與指派。
- 以固定、可驗證的 workflow 取代第一版自訂流程。
- 依 Project Role、建立人及 assignee 控制 Issue 異動。
- 保留狀態、指派與重要欄位異動歷程。
- 提供留言、搜尋、篩選與分頁，滿足日常工作追蹤。
- 封存 Project 後保留完整讀取，但禁止所有 Issue 與留言異動。

**Non-Goals:**

- 自訂 workflow、狀態、Issue type 或 priority。
- Issue 刪除、子任務、附件、標籤、watcher、通知或工時。
- Scrum sprint、看板拖曳、甘特圖、SLA 或報表。
- 文件章節、OpenAPI、資料表或程式碼關聯。
- AI 摘要、語意搜尋、Text-to-SQL 或自動狀態變更。

## Decisions

### 1. Issue 直接隸屬 Project

`Issue` 直接保存 `ProjectId`，所有 query 都從 Project membership 開始授權。跨 Project 的 Issue 存取回傳 Not Found，避免洩漏存在性。System Admin 仍須是該 Project member。

### 2. 使用固定 enum 與固定 workflow

Issue type 固定為 `Feature`、`Bug`、`ChangeRequest`；priority 固定為 `Low`、`Medium`、`High`、`Critical`；status 固定為 `Draft`、`Approved`、`InProgress`、`QA`、`Done`。

允許轉換：

- `Draft → Approved`：Owner／Manager。
- `Approved → InProgress`：Owner／Manager或目前 assignee。
- `InProgress → QA`：Owner／Manager或目前 assignee。
- `QA → Done`：Owner／Manager。
- `QA → InProgress`：Owner／Manager或目前 assignee。
- `Done → InProgress`：Owner／Manager重新開啟。

新 Issue 一律從 Draft 開始。第一版不允許任意跳轉，以免狀態失去共同語意；未來若固定流程造成實際阻礙，再以獨立 change 設計 workflow configuration。

### 3. 角色與 ownership 分工

- Owner／Manager：建立、編輯、指派任何 Project Issue，並執行管理者 workflow transitions。
- Member：建立 Issue；僅在 Draft 時編輯自己建立的 Issue；可處理指派給自己的工作轉換；可留言。
- Viewer：唯讀 Issue、歷程與留言。

Member 建立時只能不指派或指派給自己。Owner／Manager 可指派任何目前 Project member；移除 Project membership 時不自動刪除歷史 Issue，既有 assignee 改為 null 並記錄 history，避免留下不可處理的指派。

### 4. History 是 append-only 業務歷程

`IssueHistory` 記錄 ActorUserId、ChangeType、FieldName、PreviousValue、NewValue、CreatedAt。建立、狀態、assignee、type、priority、title、description 與 due date 的成功異動都應寫入 history；失敗操作不寫入。History 不提供更新或刪除 API。

ProjectAuditEvent 保留專案與 membership 稽核；IssueHistory 保存 Issue 層級業務演進，兩者不混用。

### 5. 留言第一版不可修改或刪除

`IssueComment` 保存作者、內容與建立時間。Owner、Manager、Member 可在 active Project 留言；Viewer 只讀。第一版不提供編輯／刪除，減少稽核模糊與權限分支。

### 6. 查詢採伺服器端分頁

Issue list 預設每頁 20 筆，最大 100 筆，依 UpdatedAt descending 排序。支援 title／description 關鍵字，以及 type、status、priority、assignee 篩選。查詢永遠固定 ProjectId，不提供跨 Project 全站 Issue 搜尋。

### 7. 前端沿用單頁 Project workspace

Project 詳情增加 Issues 入口，Issue list 使用 PrimeVue DataTable、篩選控制與分頁。建立／詳情使用 Dialog 或分區畫面；狀態操作只顯示伺服器允許的下一步。API 仍是安全邊界，前端隱藏按鈕只改善 UX。

## Risks / Trade-offs

- [固定 workflow 不符合所有團隊] → 第一版先建立一致語意，實際使用後另規劃自訂 workflow。
- [Member 權限可能太嚴或太鬆] → 將規則集中於 Issue authorization service，並以測試固定 creator／assignee／role 行為。
- [移除 Project member 造成懸空 assignee] → membership 移除 transaction 內清除 active assignments 並追加 history。
- [History 快速增長] → 依 IssueId、ProjectId、CreatedAt 建立索引；第一版不提供跨專案歷程查詢。
- [關鍵字 Contains 查詢在資料量大時變慢] → MVP 先用 PostgreSQL case-insensitive query；達到實際規模後再評估 trigram／full-text index。
- [多人同時更新可能覆蓋資料] → 第一版以 UpdatedAt 回傳最新狀態並保持 mutation 小型化；若出現實際衝突，再加入 concurrency token。

## Migration Plan

1. 新增 Issues、IssueHistory、IssueComments tables、foreign keys 與查詢索引。
2. 部署 API migration 與 endpoints；現有 Project 不需資料轉換。
3. 部署 frontend Issue workspace。
4. 回退應用時保留新增 tables，避免遺失使用者建立的 Issue；正式資料產生後採前向修正，不執行破壞性 rollback。

## Open Questions

- 是否需要 Project 內連號（例如 `PMS-123`），先使用 Issue Id，待實際使用後決定。
- 是否允許 Viewer 留言、Member 修改非自己建立但未指派的 Draft，以及是否需要 Issue delete，均先採較保守規則，後續依使用回饋調整。

