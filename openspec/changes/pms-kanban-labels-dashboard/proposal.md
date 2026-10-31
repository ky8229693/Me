## Why

現有 PMS 已具備 Project、Issue workflow、成員與權限基礎，但 Issue 仍主要以表格操作，Dashboard 也仍由早期 Todo 示範功能佔據，無法有效呈現使用者當前工作。此 change 將把既有資料與工作流整合為可視化 Kanban、可分類的標籤與具行動價值的 Dashboard，並移除不再屬於產品方向的 Todo 功能。

## What Changes

- 新增 Project Issue Kanban view，依固定 workflow 狀態分欄，支援權限感知的狀態移動、既有篩選條件及 Issue 詳情導覽。
- 新增 Project-scoped Issue labels，允許 Project Owner／Manager 管理標籤，並依既有 Issue 編輯規則指派或移除標籤。
- 擴充 Issue 查詢與回應，使清單及 Kanban 可顯示與篩選標籤。
- 將 Dashboard 改為使用者工作台，顯示可存取 Project 範圍內的指派工作、逾期項目、狀態摘要及最近活動，並提供對應頁面連結。
- **BREAKING** 移除 Todo API、記憶體服務、資料合約、前端 API／元件及 Todo 專用測試；`/api/todos` 不再提供。
- 更新既有測試基礎規格，以 PMS Dashboard 與 Issue 主要流程取代 Todo 測試責任。

## Capabilities

### New Capabilities

- `issue-kanban`: 定義 Project Issue Kanban 的欄位、篩選、狀態移動、權限與 URL 狀態。
- `issue-labels`: 定義 Project-scoped label 的管理、驗證、Issue 指派與查詢行為。
- `dashboard-workbench`: 定義登入使用者 Dashboard 的摘要、工作清單、最近活動與導覽行為。

### Modified Capabilities

- `issue-management`: Issue 資料與查詢新增 labels，並支援 label filter。
- `issue-page-navigation`: Project Issues 可在 list 與 board view 間切換，且 view/filter 狀態保存在 URL。
- `api-test-foundation`: 移除 Todo service／endpoint 測試要求，改由 PMS 核心 API 測試維持基礎覆蓋。
- `frontend-api-client-foundation`: 移除 Todo 專用 API 與型別要求，補入 Dashboard 與 Issue labels／Kanban 所需 API 邊界。
- `frontend-test-foundation`: 移除 Todo UI 測試要求，改為 Dashboard 與 Kanban 核心互動測試。
- `e2e-test-foundation`: 移除 Todo E2E 流程，改以 Dashboard 到 Issue 的主要工作路徑驗證登入後整合行為。

## Impact

- Backend：Issue entity／contracts／queries、Project-scoped label persistence、Dashboard read model 與 endpoints、EF Core migration，以及 Todo endpoint/service/contracts 移除。
- Frontend：Dashboard、Project Issues list/board 切換、Issue create/detail label controls、API modules、types、styles、router query handling，以及 Todo components/API 移除。
- Authorization：沿用 Project membership 存取邊界、Issue workflow transition 規則與 Issue 編輯規則；System Admin 不取得 Project bypass。
- Tests：更新 xUnit、Vitest 與 Playwright coverage，刪除 Todo-specific fixtures/assertions，新增 labels、Kanban、Dashboard 與授權案例。
- API：`/api/todos` 為明確移除的舊示範端點；新增或擴充 Project Issue、label 與 Dashboard read endpoints。
