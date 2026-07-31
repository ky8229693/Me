## Purpose

定義 Admin 安全維護 Identity 自訂 Roles 的 API、驗證規則與前端 Role Management 操作面。
## Requirements
### Requirement: 授權管理者可查詢 Role 目錄
API SHALL 在 `/api/admin/roles` 提供受對應 Role management Permission policy 保護的 Role 查詢 endpoint，並以 shared response 回傳 role id、name、system/custom 標記、目前使用者數量與有效 Permission 摘要。

#### Scenario: 有權限者查詢 Roles
- **WHEN** 具有有效 token 與對應 Role management Permission 的使用者查詢 Role 目錄
- **THEN** API 回傳所有現存 Identity roles、各自 user count 與有效 Permission 摘要
- **THEN** response MUST NOT 暴露 normalized name、concurrency stamp 或其他 Identity 內部欄位

#### Scenario: 缺少權限者查詢 Roles
- **WHEN** authenticated user 缺少對應 Role management Permission 且呼叫 Role 管理 endpoint
- **THEN** API 回傳 HTTP 403 shared failure envelope

### Requirement: Admin 可建立有效的自訂 Role
API SHALL 允許 Admin 建立自訂 role；名稱 MUST trim 後為 2–50 個字元，只包含 Unicode letters、numbers、空白、`-`、`_`，且 MUST 以 case-insensitive 方式保持唯一。

#### Scenario: 建立自訂 Role
- **WHEN** Admin 提交合法且不存在的 role name
- **THEN** API 建立 Identity role 並回傳 HTTP 201 shared success response
- **THEN** 新 role 標記為 custom 且 user count 為 0

#### Scenario: 建立重複 Role
- **WHEN** Admin 提交與現存 role 僅大小寫不同的名稱
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** 系統不建立重複 role

#### Scenario: 建立無效或保留名稱
- **WHEN** Admin 提交不符合格式、長度限制或為 `Admin`／`User` 大小寫變形的名稱
- **THEN** API 回傳 HTTP 400 shared validation failure

### Requirement: System Roles 不可修改或刪除
系統 MUST 將 `Admin` 與 `User` 視為 system roles，且 Role 管理操作 MUST 禁止重新命名或刪除它們。

#### Scenario: 重新命名 System Role
- **WHEN** Admin 嘗試重新命名 `Admin` 或 `User`
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** role name 保持不變

#### Scenario: 刪除 System Role
- **WHEN** Admin 嘗試刪除 `Admin` 或 `User`
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** role 與所有 user-role relations 保持不變

### Requirement: Admin 可安全重新命名自訂 Role
API SHALL 允許 Admin 將自訂 role 更新為另一個合法且唯一的名稱；若 role 已指派使用者，系統 MUST 更新所有受影響使用者 security stamps。

#### Scenario: 重新命名未使用 Role
- **WHEN** Admin 將 user count 為 0 的自訂 role 改為合法唯一名稱
- **THEN** API 更新 role name 並回傳最新資料

#### Scenario: 重新命名已使用 Role
- **WHEN** Admin 將已指派一位或多位使用者的自訂 role 改為合法唯一名稱
- **THEN** 系統在同一 transaction 更新 role 與所有受影響使用者 security stamps
- **THEN** 受影響使用者的既有 JWT 在下一次受保護 request 被拒絕

#### Scenario: Rename 部分失敗
- **WHEN** role update 或任一 security stamp update 失敗
- **THEN** API 回傳 conflict failure
- **THEN** role name 與所有受影響使用者 stamps 保持 mutation 前狀態

### Requirement: Admin 只能刪除未使用的自訂 Role
API SHALL 允許 Admin 刪除 user count 為 0 的自訂 role，且 MUST 拒絕刪除任何仍被使用者指派的 role。

#### Scenario: 刪除未使用 Role
- **WHEN** Admin 刪除一個 user count 為 0 的自訂 role
- **THEN** API 刪除 role 並回傳成功 shared response

#### Scenario: 刪除使用中 Role
- **WHEN** Admin 刪除一個仍有至少一位使用者的自訂 role
- **THEN** API 回傳 HTTP 409 shared failure envelope 並指出目前 user count
- **THEN** role 與所有 user-role relations 保持不變

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
