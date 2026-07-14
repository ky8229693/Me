## 1. Role 與 Policy 基礎

- [x] 1.1 新增集中管理 `Admin`、`User` roles 與 `AdminOnly` policy 的常數
- [x] 1.2 建立使用 `RoleManager` 且可重複執行的基礎 role initializer
- [x] 1.3 在 API 啟動時執行 role initializer，並註冊要求 Admin role 的 policy
- [x] 1.4 新增 Forbidden error code 與 JWT `OnForbidden` shared envelope response

## 2. Role-aware 認證

- [x] 2.1 修改註冊流程，成功建立帳號後配置預設 `User` role
- [x] 2.2 修改登入流程，為既有無角色帳號補上預設 `User` role
- [x] 2.3 將 JWT token service 改為非同步讀取 Identity roles
- [x] 2.4 為每個 Identity role 加入標準 JWT role claim，並更新所有 callers

## 3. Admin Policy Endpoint

- [x] 3.1 新增 `GET /api/admin/status` endpoint module，回傳 shared success envelope
- [x] 3.2 使用 `AdminOnly` policy 保護 endpoint 並在 `Program.cs` wiring

## 4. Tests

- [x] 4.1 擴充 registration test，驗證使用者獲得 `User` role 且 token 包含 role claim
- [x] 4.2 新增未登入呼叫 Admin endpoint 回傳 401 envelope 的 integration test
- [x] 4.3 新增一般 User 呼叫 Admin endpoint 回傳 403 envelope 的 integration test
- [x] 4.4 新增 Admin 使用正常 login token 成功呼叫 Admin endpoint 的 integration test
- [x] 4.5 新增 role initializer 重複執行不產生重複 roles 的測試

## 5. 文件與驗證

- [x] 5.1 更新 `docs/memory.md` 的 authorization 現況、限制與後續 Admin bootstrap 待辦
- [x] 5.2 執行 `dotnet build` 與 `dotnet test WEB_Demo.sln`
- [x] 5.3 執行前端 unit、E2E tests 與 production build，確認認證流程無回歸
- [x] 5.4 執行 OpenSpec validation 並修正所有錯誤
