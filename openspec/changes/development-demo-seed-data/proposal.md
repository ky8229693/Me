## Why

目前 PMS 功能完整但新資料庫缺少能立即展示 Dashboard、Kanban、labels、workflow 與 Issue activity 的資料。以 `WEB_Demo` 自身開發工作作為可重複建立的 demo Project，可同時支援展示、手動測試與 dogfooding，而不需要每次手動輸入資料。

## What Changes

- 新增非 HTTP 的 Development demo seed command，要求指定一個已存在的 owner email。
- 建立或同步固定的 `WEB_Demo Development` Project、Project membership、產品開發 labels，以及分布於各 workflow 狀態的示範 Issues。
- 讓部分 Issues 指派給 owner、設定到期日、comments 與 history，使 Dashboard、overdue、recent activity、List filters 與 Kanban 都有可展示內容。
- Seed command 必須可重複執行，不重複建立 Project、labels、Issues 或 memberships，並保留使用者自行修改的非 seed 資料。
- Seed command 只允許在 Development environment 執行；Production、CI 與一般 API 啟動不會自動寫入 demo data。
- 不建立預設帳號或密碼；owner 必須先透過既有 register／Admin bootstrap 流程存在。

## Capabilities

### New Capabilities

- `development-demo-data`: 定義安全、明確、可重複執行的 Development PMS demo data seed 行為與資料內容。

### Modified Capabilities

<!-- None. Demo seeding does not alter existing product capability contracts. -->

## Impact

- Backend：新增 command parser、seed service、啟動分支與 Development environment guard。
- Database：寫入既有 Projects、ProjectMemberships、ProjectLabels、Issues、IssueLabels、IssueComments、IssueHistory 與 ProjectAuditEvents；不需要新 schema migration。
- Operations：development 文件新增建立 owner 與執行 demo seed 的指令。
- Tests：新增 idempotency、environment guard、missing owner 與 representative dataset tests。
