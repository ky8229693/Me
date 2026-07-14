## Purpose

Define the base Identity roles, Admin authorization policy, consistent authorization failure responses, and integration coverage for role-protected API behavior.

## Requirements

### Requirement: 基礎角色可用
API MUST 定義並初始化 `Admin` 與 `User` Identity roles，且初始化流程 MUST 可安全重複執行。

#### Scenario: 首次啟動 API
- **WHEN** API 連接到尚未包含基礎 roles 的 Identity database
- **THEN** 系統建立 `Admin` 與 `User` roles

#### Scenario: 再次啟動 API
- **WHEN** `Admin` 與 `User` roles 已存在且 API 再次啟動
- **THEN** 系統不建立重複 roles
- **THEN** API 可繼續接受 requests

### Requirement: Admin policy 保護管理 API
API SHALL 提供要求 `Admin` role 的 `AdminOnly` authorization policy，並以該 policy 保護最小管理狀態 endpoint。

#### Scenario: Admin 存取管理 endpoint
- **WHEN** 具有有效 JWT 與 `Admin` role claim 的使用者呼叫 Admin-only endpoint
- **THEN** API 回傳成功的 `ApiResponse<T>` payload

#### Scenario: 一般使用者存取管理 endpoint
- **WHEN** 具有有效 JWT 但只有 `User` role 的使用者呼叫 Admin-only endpoint
- **THEN** API 回傳 HTTP 403
- **THEN** response 是失敗的 `ApiResponse<T>` 並包含 Forbidden error code

#### Scenario: 未登入使用者存取管理 endpoint
- **WHEN** 未提供 bearer token 的 request 呼叫 Admin-only endpoint
- **THEN** API 回傳 HTTP 401
- **THEN** response 是失敗的 `ApiResponse<T>` 並包含 Unauthorized error code

### Requirement: 授權基礎具有整合測試
專案 SHALL 使用真實 ASP.NET Core authorization pipeline 驗證角色與 policy 行為。

#### Scenario: 執行後端授權測試
- **WHEN** backend test suite 執行
- **THEN** tests MUST 涵蓋 User 收到 403、未登入收到 401 與 Admin 成功存取
- **THEN** tests MUST 驗證註冊 token 包含預設 `User` role claim
