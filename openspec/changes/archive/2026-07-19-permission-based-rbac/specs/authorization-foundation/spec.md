## MODIFIED Requirements

### Requirement: Permission policy 保護管理 API
API SHALL 提供可依固定 Permission key 建立 requirement 的 Permission-based authorization policy／handler，以對應 Permission 保護一般管理 endpoints；`AdminOnly` policy SHALL 保留給明確要求 system `Admin` 的高風險操作，且未知 Permission key MUST fail-closed。

#### Scenario: 具有 Permission 存取管理 endpoint
- **WHEN** 具有有效 JWT 與 endpoint 所需 Permission claim 的使用者呼叫 Permission-protected endpoint
- **THEN** API 回傳成功的 `ApiResponse<T>` payload

#### Scenario: 缺少 Permission 存取管理 endpoint
- **WHEN** 具有有效 JWT 但缺少 endpoint 所需 Permission claim 的使用者呼叫 Permission-protected endpoint
- **THEN** API 回傳 HTTP 403
- **THEN** response 是失敗的 `ApiResponse<T>` 並包含 Forbidden error code

#### Scenario: 未登入使用者存取管理 endpoint
- **WHEN** 未提供 bearer token 的 request 呼叫 Permission-protected endpoint
- **THEN** API 回傳 HTTP 401
- **THEN** response 是失敗的 `ApiResponse<T>` 並包含 Unauthorized error code

#### Scenario: System Admin 高風險操作
- **WHEN** endpoint 明確要求 `AdminOnly` 且 caller 只有自訂 Role Permissions 而不具有 `Admin` role
- **THEN** API 回傳 HTTP 403 shared failure envelope

### Requirement: 授權基礎具有整合測試
專案 SHALL 使用真實 ASP.NET Core authorization pipeline 驗證 Permission、role 與 policy 行為。

#### Scenario: 執行後端授權測試
- **WHEN** backend test suite 執行
- **THEN** tests MUST 涵蓋具有所需 Permission 時成功、缺少 Permission 時收到 403、未登入收到 401、未知 Permission fail-closed 與 system Admin 取得完整 Permissions
- **THEN** tests MUST 驗證註冊 token 包含預設 `User` role 及其目前有效 Permission claims
