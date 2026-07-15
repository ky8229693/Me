## Context

現有 Identity schema 已包含 `AspNetRoles` 與 `AspNetUserRoles`，API 啟動時建立固定 `Admin`、`User`，帳號管理 service 以程式碼 allowlist 限制只能指派這兩個 roles。JWT 會攜帶目前 roles 與 security stamp，後端在每次 authenticated request 驗證 stamp，因此 role catalog mutation 必須與使用者 token 撤銷協調。

本 change 橫跨 RoleManager/UserManager、交易與 concurrency、Admin Minimal APIs、帳號角色指派、PrimeVue views 及 E2E。自訂 roles 在第一版只代表可指派的 Identity role 名稱；它們不會自動建立 API policies 或 permissions。

## Goals / Non-Goals

**Goals:**

- 讓 Admin 安全查詢、建立、重新命名與刪除自訂 roles。
- 永久保護 `Admin`、`User` system roles。
- 防止刪除仍有使用者的 role，不做隱含 cascade mutation。
- Role rename 後立即撤銷所有受影響使用者的舊 JWT。
- 讓帳號管理可動態指派任何現存 role，保留必要 `User` 與 Admin 安全規則。
- 提供符合現有 PrimeVue 後台的 Role Management UI 與測試。

**Non-Goals:**

- 不提供 Permission model、role-permission mapping、role hierarchy 或 resource-based authorization。
- 不新增 role description、display name、color、排序欄位或 migration。
- 不允許重新命名或刪除 `Admin`、`User`。
- 不提供 cascade delete、bulk role replacement 或多租戶角色。
- 不因建立自訂 role 自動註冊 ASP.NET Core authorization policy。

## Decisions

### 沿用 IdentityRole，不新增自訂 entity

第一版只管理 `IdentityRole.Name`，以 `RoleManager<IdentityRole>` 建立、查詢與更新，並用 `AppDbContext.UserRoles` 計算使用者數量。API response 額外提供由名稱推導的 `IsSystem`，不將 system flag 寫入 database。

替代方案是導入自訂 role entity 與 description/system columns；會需要更換 Identity 泛型、migration 與較大的資料模型調整，對第一版沒有必要。

### 名稱使用 Unicode letters/numbers 的受限格式

輸入先 trim，長度限制 2–50 個字元，只接受 Unicode letters、numbers、單一或連續空白、`-`、`_`。使用 Identity normalizer 與 `RoleExistsAsync` 確保 case-insensitive uniqueness；`Admin`、`User` 的任何大小寫變形都視為保留名稱。

允許 Unicode 可支援中文組織角色；限制其他符號可降低 UI、logs 與 policy strings 的歧義。第一版不建立獨立 display name，因此 role name 同時是 JWT claim value。

### System roles 由集中常數判斷並永久保護

`AppRoles` 提供 `IsSystemRole` helper，所有 role mutation 與 response 都使用同一判斷。建立保留名稱回傳 validation；rename/delete system role 回傳 conflict。API 啟動 initializer 仍確保兩個 system roles 存在。

將 system flag 存入資料庫可支援未來更多內建 roles，但目前只有兩個穩定常數，額外 schema 狀態反而可能與程式碼失去同步。

### 使用中的 role 禁止刪除

Delete 在 transaction 中重新載入 role、確認非 system，並查詢 `AspNetUserRoles` 使用數量。數量大於零回傳 HTTP 409 與最新 user count；只有未使用 role 才呼叫 `RoleManager.DeleteAsync`。

替代方案是自動從所有使用者移除後刪除；這會一次改變多人 token 與權限，且失敗回復較複雜，因此第一版要求 Admin 先在 User Management 移除指派。

### Rename 與受影響使用者 security stamps 使用同一 transaction

Rename 在 serializable transaction 中驗證新名稱、取得目前 user ids、更新 role，再逐一 `UpdateSecurityStampAsync`。任一步驟失敗即 rollback；成功後舊 JWT 因 stamp 不符而立即失效。未指派 role 不需要 stamp 更新。

Identity role name 同時是 JWT role claim；若只更新 role row，舊 token 仍會攜帶舊名稱直到到期，因此必須主動撤銷。

### 帳號角色指派改查資料庫 role catalog

`SetRolesAsync` 先 normalize/deduplicate request，再以 `RoleManager`／role query 驗證每個名稱存在，不再使用 `[Admin, User]` 固定 allowlist。仍強制包含 `User`，並保留自我移除 Admin、最後啟用 Admin、transaction 與 stamp update。

前端 Role API 集中提供 role catalog；User Management 與 Role Management 共用 TypeScript contract。若 catalog 載入失敗，角色編輯 disabled 並顯示錯誤，不退回寫死 options。

### Role Management 使用獨立 Admin view

新增 Role Management navigation 與 view，使用 DataTable 顯示 name、system/custom、user count 與 actions；建立與 rename 使用 Dialog 或 inline form，刪除使用確認。System roles 與使用中 roles 的不允許操作以 disabled state 與後端錯誤雙重呈現。

Role CRUD 與 User Management 分頁分開，避免帳號清單承擔 catalog 維護責任；前端顯示限制仍不取代 AdminOnly API policy。

## Risks / Trade-offs

- [Role rename 需更新大量使用者 stamps] → 第一版採逐一更新與 transaction，適用目前管理規模；有實際大量角色成員時再評估批次 token version。
- [Serializable transaction 與並行指派產生 contention] → mutation 很少且優先正確性；捕捉 concurrency failure 並回傳 conflict，讓 Admin reload 後重試。
- [自訂 role 沒有 Permission 容易造成期待落差] → UI 與文件明確標示 role 只供分類與未來 policy 使用，本 change 不宣稱自動授權。
- [Role 名稱直接進入 JWT] → 使用受限字元與長度、集中驗證，不允許任意控制字元或標點。
- [使用人數在 list 後可能立即變化] → Delete 在 transaction 內重新檢查，不依賴前端顯示的 count。
- [Role options 與帳號清單分兩次 request] → UI 將 loading/error 分開處理；第一版不加入 catalog cache，規模成長後再評估。

## Migration Plan

1. 新增 role contracts、validation、system role helper 與 administration service tests。
2. 新增 `/api/admin/roles` endpoints 與 integration tests，先驗證 system/assigned role 安全規則及 rename token 撤銷。
3. 調整 account role assignment 使用動態 role catalog，執行既有最後 Admin regression tests。
4. 新增前端 Role API、Role Management view/navigation，並讓 User Management 動態載入 options。
5. 新增 unit/E2E tests、更新文件，執行 backend/frontend/E2E/build/OpenSpec validation。

回復時先停用 Role Management UI/endpoints，再將帳號指派恢復固定 allowlist。已建立的自訂 `AspNetRoles` rows 不會影響舊程式，但回復前應確認使用者沒有仍依賴的自訂 role 指派；不需要 schema rollback。

## Open Questions

- 未來 Permission model 應採 role-policy mapping、claims 或 resource-based authorization，需由獨立 change 決定。
- 若角色數量或成員數大幅增加，Role list user count 與 rename stamp updates 是否需要 pagination/batch operation，待實際規模再評估。
