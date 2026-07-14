## Why

目前 API 只能區分已登入與未登入，無法限制只有特定角色才能存取的功能。建立最小的 Role／Policy 基礎，可在未來加入管理功能前先固定 JWT 角色宣告、403 回應與授權測試行為。

## What Changes

- 建立共用 `Admin`、`User` role 名稱與 `AdminOnly` policy 名稱。
- API 啟動時確保基礎 roles 存在於 ASP.NET Core Identity。
- 新註冊帳號預設加入 `User` role；既有無角色帳號登入時補上預設 role。
- JWT 加入使用者 role claims，讓 ASP.NET Core policy authorization 可判斷角色。
- 建立最小的 Admin-only API endpoint，用來驗證 Role／Policy 授權管線。
- 讓未登入與角色不足分別回傳一致的 `ApiResponse<T>` 401／403 payload。
- 加入角色配置、JWT claims 與 401／403／Admin 成功的 integration tests。
- 不加入管理角色 UI、Todo ownership 或通用 Permission 資料模型。

## Capabilities

### New Capabilities

- `authorization-foundation`: 定義基礎 roles、Admin policy、受保護 endpoint、Forbidden response 與授權測試行為。

### Modified Capabilities

- `auth-foundation`: 註冊／登入需要配置預設 role，JWT 需要包含 Identity roles。

## Impact

- 後端新增 authorization constants、role initializer 與 Admin endpoint module。
- `Program.cs` 增加 role 初始化、Admin policy 與 JWT forbidden response 設定。
- JWT token service 改為非同步讀取 Identity roles。
- Identity 現有 schema 已包含 role tables，本 change 不需要新增 migration。
- 後端 integration tests 增加角色與 policy 情境；前端 API/UI 本批不變。
