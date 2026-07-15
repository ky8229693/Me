## Why

目前帳號管理只接受固定 `Admin`、`User` roles，無法建立符合實際組織需求的角色名稱，也無法在後台維護角色目錄。加入受保護的自訂 Role CRUD，可讓 Admin 管理角色並指派給使用者，同時維持系統角色與既有授權安全界線。

## What Changes

- 新增 Admin-only Role 管理 API，支援查詢、建立、重新命名及刪除自訂 roles。
- Role 清單回傳 system/custom 標記與目前使用者數量，不暴露 Identity concurrency 等內部欄位。
- 將 `Admin`、`User` 定義為不可重新命名或刪除的 system roles。
- 自訂 role 名稱加入 trim、長度、允許字元、case-insensitive uniqueness 與保留名稱驗證。
- 已被任何使用者指派的 role 不允許刪除，回傳 conflict；第一版不提供 cascade removal。
- 重新命名已使用的 role 後更新所有受影響使用者 security stamp，使舊 JWT 立即失效。
- 新增 PrimeVue Role Management 頁面，提供 role list、create、rename、delete confirmation、使用人數與 system role 保護狀態。
- 帳號管理的角色選項改由 API 動態載入，允許指派任何現存 role，同時保留必要 `User`、自我降級及最後 Admin 保護。
- 本批只管理 Identity role 名稱與指派，不加入 Permission CRUD、role-permission mapping、role hierarchy、description 或多租戶角色。

## Capabilities

### New Capabilities

- `role-management`: 定義 Admin 管理自訂 Identity roles、名稱驗證、system role 保護、使用中刪除限制、rename token 撤銷與 PrimeVue 管理頁。

### Modified Capabilities

- `role-assignment`: 將可指派範圍由固定 `Admin`／`User` 改為所有現存 Identity roles，前端選項由 Role API 動態載入，並保留既有安全不變量。

## Impact

- 後端新增 role administration contracts、service 與 `/api/admin/roles` Minimal API endpoints，使用既有 `RoleManager<IdentityRole>`、`UserManager<IdentityUser>` 與 `AppDbContext` transaction。
- 帳號角色指派 service 改為查詢目前 role catalog，不再以程式碼固定 allowlist 驗證全部 roles。
- 前端新增 Role API module、Role Management view、Admin navigation，並調整 User Management role options。
- Identity 既有 schema 已包含 role name、normalized name 與 user-role relation；第一版不新增欄位，預期不需要 migration。
- 後端、前端 unit 與 Playwright E2E tests 增加 Role CRUD、system role 保護、使用中刪除與動態指派情境。
