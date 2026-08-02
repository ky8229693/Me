## 1. Label Persistence and Contracts

- [x] 1.1 新增 `ProjectLabel`、`IssueLabel` entities、DbContext relationships、Project 內 normalized name unique index 與查詢所需 indexes
- [x] 1.2 建立並檢查 EF Core migration，確認升級建立 label tables／constraints 且不變更既有 Issue 資料
- [x] 1.3 新增 label CRUD、Issue label assignment 與 label-bearing Issue response contracts，包含 name、color、ID 與跨 Project validation

## 2. Label Authorization and APIs

- [x] 2.1 實作 Project label administration service，限制 Owner／Manager 與 active Project，並以 transaction 處理已指派 label 的刪除與 Issue history
- [x] 2.2 擴充 Issue create/update/query service，使 label assignment 沿用 Issue edit authorization，並支援多 label AND filter
- [x] 2.3 新增 Project label Minimal API endpoints、Swagger metadata 與 `ApiResponse<T>` handling
- [x] 2.4 新增 label service／endpoint integration tests，涵蓋 manager CRUD、Viewer denial、duplicate name、archive、cross-Project assignment 與 deletion audit

## 3. Kanban Backend

- [x] 3.1 建立共用 Issue filter normalization，讓 paginated list 與 board query 一致處理 search、type、status、priority、assignee 及 labels
- [x] 3.2 新增 bounded Project board read endpoint 與 card projection，回傳固定 workflow 狀態、allowed transitions、total 及 truncation metadata
- [x] 3.3 新增 board endpoint integration tests，涵蓋 membership isolation、filters、fixed ordering／projection、result bound 與既有 transition authorization

## 4. Kanban and Label Frontend

- [x] 4.1 擴充 frontend Issue types/API modules，新增 label CRUD／assignment、label filter 與 board response functions
- [x] 4.2 在 Project Issues route 加入 List／Board switch，將 view 與 compatible filters 同步至 route query，切換 Board 時清除 pagination semantics
- [x] 4.3 建立 responsive Kanban columns/cards、truncation state、Issue detail navigation，以及使用 server-provided transitions 的 keyboard-accessible move action
- [x] 4.4 加入 native pointer drag enhancement、pending state 與失敗時 authoritative refresh／rollback，不新增 drag-and-drop runtime dependency
- [x] 4.5 在 Issue create/detail 加入 label selection，並提供 Owner／Manager 可用的 Project label management UI
- [x] 4.6 新增 Vitest coverage，涵蓋 route state、filter retention、card navigation、label controls、permitted move 與 failed move rollback

## 5. Dashboard Workbench

- [x] 5.1 實作 membership-scoped Dashboard read service，以 UTC 計算 assigned、overdue、status counts，並投影 bounded work 與 recent activity lists
- [x] 5.2 新增 authenticated `/api/dashboard` endpoint、response contracts、query indexes 與 integration tests，驗證 Project membership isolation、archived Project handling、ordering及 empty states
- [x] 5.3 新增 frontend Dashboard API module/types，將 `DashboardView` 改為摘要 cards、assigned/overdue work、status overview 與 recent activity
- [x] 5.4 實作 Dashboard loading、empty、error/retry states及 Project／Issue deep links，並新增 API 與 component unit tests

## 6. Todo Removal and Test Migration

- [x] 6.1 移除 Todo endpoint mapping、DI registration、service/contracts 與相關 backend unit/integration tests，並驗證 `/api/todos` 不再公開
- [x] 6.2 移除 frontend Todo API/types/components、Dashboard Todo state/styles 及 Todo-specific Vitest assertions
- [x] 6.3 以 Dashboard → Issue → filtered Board → permitted transition 的 Playwright flow 取代 Todo E2E spec，保留既有 isolated database lifecycle
- [x] 6.4 更新 README、development、memory 與測試文件，移除 Todo 功能敘述並記錄 Dashboard、labels 與 Kanban 行為

## 7. Verification

- [x] 7.1 執行 migration／schema review、`dotnet test WEB_Demo.sln` 與 API Release build
- [x] 7.2 執行 frontend unit tests、TypeScript production build 與 Playwright E2E suite
- [x] 7.3 執行 OpenSpec strict validation 與 targeted Kanban E2E；Compose config 已於本機通過，production container builds 依專案 local-or-CI policy 交由未修改部署設定的 CI `Containers` job 驗證
