## Context

現有 `auth-foundation` 已使用 ASP.NET Core Identity、PostgreSQL 與 JWT，Identity schema 也已包含 roles tables；但 token 目前只有 user id 與 email claims，API 只使用一般 authenticated policy。這次變更跨越 Identity role 初始化、註冊／登入、token issuance、authorization middleware、endpoint wiring 與 integration tests，但不需要改動資料庫 schema。

## Goals / Non-Goals

**Goals:**

- 提供一致的 `Admin`、`User` roles 與 `AdminOnly` policy 常數。
- 確保 API 啟動後基礎 roles 存在。
- 讓新帳號及既有無角色帳號至少擁有 `User` role。
- 將 Identity roles 寫入 JWT role claims。
- 以最小 Admin-only endpoint 驗證 401、403 與成功授權。
- 使用 shared `ApiResponse<T>` 回傳 authorization failure。

**Non-Goals:**

- 不提供角色管理 UI 或公開的升降權 API。
- 不建立動態 Permission table、claims 管理後台或多租戶授權。
- 不處理 Todo ownership；該需求留給 Todo persistence change。
- 不建立預設 Admin 帳密或把敏感 credentials 放進設定。

## Decisions

### 以常數集中 role 與 policy 名稱

新增 `Authorization/AppRoles.cs` 與 `Authorization/AppPolicies.cs`，避免 endpoints、token service 與 tests 使用散落字串。`Admin` 與 `User` 採穩定名稱，`AdminOnly` policy 使用 `RequireRole(AppRoles.Admin)`。

替代方案是直接在每個 endpoint 寫字串；規模雖小，但拼字錯誤會造成難以察覺的授權失敗。

### API 啟動時 idempotently 初始化 roles

新增 scoped role initializer，透過 `RoleManager<IdentityRole>` 檢查並建立缺少的 roles。初始化在 app build 後、接受 request 前執行；重複啟動不會建立重複資料。

替代方案是把 roles 寫入 migration seed。Identity role normalized name 與 concurrency stamp 較適合由 Identity API 管理，而且現有 schema 不需要 migration。

### 註冊與無角色登入帳號配置 User role

註冊成功後立即 `AddToRoleAsync(User)`；登入時若 `GetRolesAsync` 為空，補上 `User` role。這可讓既有 local 帳號平滑遷移，不需要一次性資料修正程式。

如果 role 指派失敗，認證流程回傳 server error，而不是簽發缺少預期角色的 token。

### Token service 非同步讀取 roles

`IJwtTokenService.CreateTokenAsync` 透過 `UserManager.GetRolesAsync` 取得 roles，並為每個 role 加入 `ClaimTypes.Role`。使用 ASP.NET Core 預設 role claim type，可直接搭配 `RequireRole`，不需自訂 claims transformation。

替代方案是由 endpoint 讀取 roles 再傳給 token service；這會讓 token claim 組裝責任散落到不同 callers。

### Admin endpoint 僅作為最小可驗證能力

新增 `GET /api/admin/status`，回傳 shared success envelope 並要求 `AdminOnly` policy。此 endpoint 不執行管理操作，避免在基礎 change 中引入額外業務功能。

Integration tests 直接使用 test DI scope 的 `UserManager` 配置 Admin role，再透過正常 login 取得 token；production 不建立預設 Admin 帳密。

### JWT events 統一 401 與 403 envelope

保留 `OnChallenge` 的 401 JSON，新增 `OnForbidden` 回傳 403 與 `Forbidden` error code。如此 browser/API clients 不需要針對空 response 特例處理。

## Risks / Trade-offs

- [啟動時資料庫不可用會阻止 API 啟動] → Role 是 authorization 必要資料；讓啟動明確失敗比服務以不完整 roles 運作安全。
- [既有無角色帳號直到登入才補 role] → 登入流程 idempotently 補上 `User`，不需要批次 migration。
- [Role 變更後舊 JWT 仍保留舊 claims] → 接受 JWT 在有效期限內的快取語意；角色調整後需重新登入才取得新 token。
- [Admin-only status endpoint 未提供實際業務價值] → 它是可移除或被未來管理 endpoint 取代的最小授權驗證面。
- [Role assignment 失敗留下已建立但無角色的新帳號] → 回傳明確錯誤並記錄；後續登入會再次嘗試補上預設 role。

## Migration Plan

1. 新增 role/policy constants 與 role initializer。
2. 設定 Admin policy、403 response 並在啟動時初始化 roles。
3. 修改註冊、登入與 token service，配置及簽發 role claims。
4. 新增 Admin endpoint 與 integration tests。
5. 啟動 API 後確認 `Admin`、`User` roles 已存在；既有帳號於下次登入補上 `User`。

回復時可移除 policy、initializer、endpoint 與 role-aware token 邏輯；Identity roles table 中留下的基礎 role rows 不影響舊版程式。

## Open Questions

- 未來建立管理功能時，再決定第一個 production Admin 的安全 bootstrap 流程。
- Todo ownership change 再決定採 role、resource-based authorization 或兩者並用。
