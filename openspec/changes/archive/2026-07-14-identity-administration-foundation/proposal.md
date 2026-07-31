## Why

目前系統已有 `Admin`、`User` roles 與 `AdminOnly` policy，但缺少安全建立第一位 Admin、管理既有帳號及調整角色的操作面；若直接進入管理功能，系統既無正式管理者入口，也無法可靠停用帳號或收回權限。

## What Changes

- 提供一次性的本機／部署 CLI 流程，安全建立或提升第一位 Admin，不內建預設 Admin 帳密，也不開放匿名 bootstrap API。
- 新增 Admin-only 帳號查詢能力，支援分頁、搜尋、查看帳號狀態與現有 roles。
- 新增 Admin-only 帳號啟用／停用能力，並避免管理者停用自己或停用最後一位可用 Admin。
- 新增 Admin-only 固定角色指派能力，只允許既有 `Admin`、`User` roles，並避免自我降級或移除最後一位 Admin。
- 帳號停用或角色異動時更新使用者 security stamp；JWT 驗證時檢查帳號狀態與 security stamp，使既有 token 立即失效。
- 新增 PrimeVue 帳號管理頁面，提供搜尋、分頁、狀態切換與角色指派操作。
- 加入 bootstrap、帳號管理、角色安全規則、token 失效及前端主要操作的測試。
- 本批不提供自訂角色 CRUD、Permission model、Email 寄送、忘記密碼流程、硬刪除帳號或使用者個別 Permission。

## Capabilities

### New Capabilities

- `admin-bootstrap`: 定義第一位 Admin 的安全 CLI 建立／提升流程、輸入限制與可重複執行行為。
- `account-management`: 定義 Admin 查詢、搜尋、分頁、啟用及停用帳號的 API 與前端管理行為。
- `role-assignment`: 定義 Admin 指派固定 roles、禁止危險自我操作及保護最後一位 Admin 的行為。

### Modified Capabilities

- `auth-foundation`: JWT 驗證新增帳號啟用狀態與 security stamp 檢查，使停用帳號或角色異動後的既有 token 失效。

## Impact

- 後端新增 bootstrap command、Admin user endpoint module、管理用 contracts/services，以及 Identity 帳號狀態與角色操作。
- JWT issuance 與 validation 增加 security stamp claim／驗證；受保護 request 會查詢目前 Identity 使用者狀態。
- 前端新增 Admin-only 帳號管理 API client、頁面與導覽入口，並依目前登入者角色控制顯示。
- Identity 現有 schema 已包含 roles、lockout 與 security stamp 欄位，預期不需要新增 migration。
- 後端 integration tests、前端 unit tests 與 Playwright E2E tests 會增加管理情境。
