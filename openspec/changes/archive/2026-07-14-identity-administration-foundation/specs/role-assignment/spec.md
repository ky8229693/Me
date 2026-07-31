## ADDED Requirements

### Requirement: Admin 可指派系統固定角色
API SHALL 允許 Admin 以冪等方式替其他帳號設定 roles，且只接受系統定義的 `Admin` 與 `User` roles。

#### Scenario: 將 User 提升為 Admin
- **WHEN** Admin 將啟用中的一般帳號 roles 設為 `User` 與 `Admin`
- **THEN** API 配置缺少的 roles 並回傳更新後帳號資料
- **THEN** 系統更新目標帳號的 security stamp

#### Scenario: 重複提交相同角色
- **WHEN** Admin 提交與目標帳號現況相同的 roles
- **THEN** API 回傳成功且不建立重複 role 關聯

#### Scenario: 提交未知角色
- **WHEN** role 指派 request 包含 `Admin`、`User` 以外的 role
- **THEN** API 回傳 HTTP 400 shared validation failure
- **THEN** 目標帳號原有 roles 保持不變

### Requirement: 角色異動保護管理存取
角色指派流程 MUST 防止管理者自我降級及移除最後一位啟用中 Admin，並 MUST 確保所有帳號至少保留 `User` role。

#### Scenario: Admin 嘗試移除自己的 Admin role
- **WHEN** Admin request 會移除目前登入帳號的 `Admin` role
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** 目前登入帳號的 roles 保持不變

#### Scenario: 移除最後一位啟用中 Admin
- **WHEN** role request 會使系統不再具有任何啟用中的 Admin
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** 目標帳號的 roles 保持不變

#### Scenario: Request 未包含 User role
- **WHEN** Admin 提交未包含 `User` 的合法 role 集合
- **THEN** API 回傳 HTTP 400 shared validation failure
- **THEN** 目標帳號的 roles 保持不變

### Requirement: 前端提供固定角色指派操作
帳號管理頁 SHALL 允許 Admin 查看並設定其他帳號的固定 roles，且 SHALL 清楚顯示禁止自我降級與最後 Admin 保護所造成的錯誤。

#### Scenario: Admin 更新其他帳號角色
- **WHEN** Admin 在帳號管理頁選擇合法 roles 並確認儲存
- **THEN** 前端透過集中式 API client 提交角色設定
- **THEN** 成功後畫面顯示後端回傳的最新 roles
