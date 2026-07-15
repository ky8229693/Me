## 1. Role Contracts 與安全基礎

- [x] 1.1 新增 Role list/create/update response 與 request contracts，排除 Identity 內部欄位
- [x] 1.2 擴充 `AppRoles` system role 判斷，集中保護 `Admin`、`User` 與保留名稱
- [x] 1.3 建立 Role name validator，支援 trim、Unicode letters/numbers、空白、`-`、`_`、2–50 長度與 case-insensitive uniqueness
- [x] 1.4 新增 validator 與 system role helper unit tests，涵蓋中文、大小寫重複、保留名稱及無效字元

## 2. Role Administration Service

- [x] 2.1 建立 scoped role administration service，查詢 roles、system/custom 標記與 `AspNetUserRoles` user count
- [x] 2.2 實作自訂 role create，使用 `RoleManager` normalized uniqueness 並映射 validation/conflict failures
- [x] 2.3 實作 system role rename/delete 保護與 role not found 結果
- [x] 2.4 實作自訂 role delete，在 transaction 重新確認 user count 並拒絕刪除使用中 role
- [x] 2.5 實作自訂 role rename，在 transaction 更新 role 與所有受影響使用者 security stamps，失敗時 rollback
- [x] 2.6 新增 service tests，涵蓋 list/count、create、duplicate、system role 保護、使用中刪除、rename 與 rollback

## 3. Admin Role APIs

- [x] 3.1 新增 `/api/admin/roles` Minimal API endpoints，包含 GET、POST、PUT `/{id}`、DELETE `/{id}`
- [x] 3.2 所有 Role endpoints 套用 `AdminOnly` policy 與 shared envelope，正確映射 201、400、403、404、409
- [x] 3.3 新增 integration tests，驗證 Admin CRUD、一般 User 403、敏感欄位排除、system role 與使用中 role conflicts
- [x] 3.4 新增 rename token revocation integration test，確認受影響使用者舊 JWT 在下一個 request 失效

## 4. 動態 Role Assignment

- [x] 4.1 修改帳號角色指派 service，改以目前 Identity Role 目錄驗證 requested roles 並支援自訂 role
- [x] 4.2 保留必要 `User`、自我移除 Admin、最後啟用 Admin、冪等更新、transaction 與 security stamp 規則
- [x] 4.3 擴充後端 tests，涵蓋指派/移除自訂 role、提交不存在 role、role rename 後指派與既有安全 regression

## 5. PrimeVue Role Management

- [x] 5.1 新增集中式 frontend Role API client 與 TypeScript contracts，涵蓋 list/create/rename/delete
- [x] 5.2 新增 Admin-only Role Management navigation 與 view state wiring
- [x] 5.3 使用 PrimeVue 建立 Role Management 清單，顯示 name、system/custom、user count、loading、empty 與 error states
- [x] 5.4 建立自訂 role create、rename、delete confirmation 操作，並 disabled system role 與使用中 role 的不允許 actions
- [x] 5.5 修改 User Management 從 Role API 動態載入 options；載入失敗時顯示錯誤並禁止儲存 roles
- [x] 5.6 新增前端 unit tests，涵蓋 Role API、navigation、CRUD feedback、system/assigned role restrictions 與動態 options failure

## 6. E2E、文件與驗證

- [x] 6.1 新增 Playwright Role Management happy path，驗證建立、指派、使用中刪除拒絕、移除指派、rename 與 delete
- [x] 6.2 新增最小安全 E2E，驗證一般 User 看不到入口且直接 Role API request 收到 403
- [x] 6.3 更新 `docs/memory.md` 與 UI/development 文件，說明自訂 role 不自動授予 Permission、system role 保護與刪除限制
- [x] 6.4 執行 `dotnet test WEB_Demo.sln`、frontend unit tests、Playwright E2E 與 production build
- [x] 6.5 執行 OpenSpec strict validation 並修正所有錯誤
