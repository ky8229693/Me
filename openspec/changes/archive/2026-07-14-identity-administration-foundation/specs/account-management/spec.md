## ADDED Requirements

### Requirement: Admin 可查詢帳號清單與詳細資料
API SHALL 在 `/api/admin/users` 下提供受 `AdminOnly` policy 保護的帳號查詢 endpoints，並以 `ApiResponse<T>` 回傳分頁結果、基本帳號資料、啟用狀態與 roles。

#### Scenario: Admin 查詢分頁帳號
- **WHEN** 具有有效 Admin token 的使用者指定合法 page、page size 與選用搜尋字串
- **THEN** API 回傳符合 Email 搜尋條件的帳號分頁、總筆數、啟用狀態與 roles
- **THEN** payload MUST NOT 包含 password hash、security stamp 或其他 Identity 敏感欄位

#### Scenario: 一般 User 查詢帳號
- **WHEN** 只有 `User` role 的使用者呼叫帳號管理 endpoint
- **THEN** API 回傳 HTTP 403 shared failure envelope

#### Scenario: 查詢不存在的帳號
- **WHEN** Admin 以不存在的 user id 查詢詳細資料
- **THEN** API 回傳 HTTP 404 shared failure envelope

### Requirement: Admin 可停用與重新啟用帳號
API SHALL 允許 Admin 設定其他帳號的啟用狀態，且狀態變更 MUST 更新該帳號的 security stamp。

#### Scenario: 停用一般帳號
- **WHEN** Admin 停用一個目前啟用的 User 帳號
- **THEN** API 將帳號設為停用並回傳成功的 shared response
- **THEN** 該帳號既有 token 與後續登入 MUST 被拒絕

#### Scenario: 重新啟用帳號
- **WHEN** Admin 重新啟用已停用帳號
- **THEN** API 將帳號設為啟用
- **THEN** 使用者 MUST 重新登入取得新的 token

#### Scenario: Admin 嘗試停用自己
- **WHEN** Admin 對目前登入的帳號提出停用要求
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** 帳號狀態保持不變

#### Scenario: 停用最後一位可用 Admin
- **WHEN** 操作會使系統不再具有任何啟用中的 Admin
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** 帳號狀態保持不變

### Requirement: 前端提供 Admin 帳號管理操作面
前端 SHALL 使用 PrimeVue 提供僅對 Admin 顯示的帳號管理入口、搜尋、分頁、狀態顯示與啟用／停用操作，並透過集中式 API client 呼叫後端。

#### Scenario: Admin 開啟帳號管理頁
- **WHEN** 目前登入者具有 `Admin` role 並開啟帳號管理頁
- **THEN** 前端顯示可搜尋與分頁的帳號清單及角色、狀態操作

#### Scenario: 一般 User 使用後台
- **WHEN** 目前登入者沒有 `Admin` role
- **THEN** 前端不顯示帳號管理導覽入口
- **THEN** 後端仍 MUST 以 authorization policy 阻止其直接呼叫管理 API
