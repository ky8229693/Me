## MODIFIED Requirements

### Requirement: 授權管理者可查詢 Role 目錄
API SHALL 在 `/api/admin/roles` 提供受對應 Role management Permission policy 保護的 Role 查詢 endpoint，並以 shared response 回傳 role id、name、system/custom 標記、目前使用者數量與有效 Permission 摘要。

#### Scenario: 有權限者查詢 Roles
- **WHEN** 具有有效 token 與對應 Role management Permission 的使用者查詢 Role 目錄
- **THEN** API 回傳所有現存 Identity roles、各自 user count 與有效 Permission 摘要
- **THEN** response MUST NOT 暴露 normalized name、concurrency stamp 或其他 Identity 內部欄位

#### Scenario: 缺少權限者查詢 Roles
- **WHEN** authenticated user 缺少對應 Role management Permission 且呼叫 Role 管理 endpoint
- **THEN** API 回傳 HTTP 403 shared failure envelope

### Requirement: 前端提供 Role Management 操作面
前端 SHALL 使用 PrimeVue 提供依有效 Permissions 顯示的 Role Management 入口與頁面，支援清單、建立、重新命名、刪除確認、system/custom 狀態、user count、Permission 摘要與 Permission 設定入口。

#### Scenario: 有權限者管理自訂 Roles
- **WHEN** 具有對應 Role management Permission 的使用者開啟 Role Management 頁面
- **THEN** 前端顯示 Role 目錄及其被授權的 create、rename、delete 與 Permission 設定操作
- **THEN** system role 與使用中 role 的禁止操作具有明確 disabled 或說明狀態

#### Scenario: Role mutation 成功
- **WHEN** 有權限使用者成功建立、重新命名或刪除自訂 role
- **THEN** 前端以後端回傳資料更新清單並顯示成功回饋

#### Scenario: Role mutation 衝突
- **WHEN** 後端因重複名稱、system role 或使用中 role 拒絕操作
- **THEN** 前端顯示可理解的錯誤且保留最新 server state

#### Scenario: 僅有 Permission 檢視權限
- **WHEN** 使用者只能查看 Role 與 Permission 資訊但沒有 mutation Permission
- **THEN** 前端顯示允許的唯讀資訊並隱藏或停用未授權操作
- **THEN** 後端仍拒絕其未授權 mutation request
