# permission-management Specification

## Purpose
TBD - created by archiving change permission-based-rbac. Update Purpose after archive.
## Requirements
### Requirement: 系統維護固定 Permission catalog
系統 SHALL 由程式碼定義具有唯一穩定 key、名稱與說明的 Permission catalog，並 MUST 以可安全重複執行的初始化流程同步至 PostgreSQL；管理 API MUST NOT 提供建立、重新命名或刪除 Permission definition 的操作。

#### Scenario: 首次初始化 Permission catalog
- **WHEN** API 啟動且資料庫尚未包含目前 catalog
- **THEN** 系統建立所有固定 Permission records
- **THEN** 每個 Permission key 在資料庫中保持唯一

#### Scenario: 再次初始化相同 catalog
- **WHEN** API 對已同步的 catalog 再次執行初始化
- **THEN** 系統不建立重複 Permission records
- **THEN** 既有 Role–Permission mappings 保持不變

#### Scenario: 未知 Permission key
- **WHEN** authorization 或管理 request 引用不在目前程式 catalog 的 Permission key
- **THEN** 系統 MUST fail-closed 且不得授予存取權

### Requirement: 系統保存 Role–Permission mappings
系統 SHALL 以具外鍵與唯一性約束的 PostgreSQL 關聯保存 Identity Role 與 Permission 的多對多 mappings，並 MUST 防止重複或孤立關聯。

#### Scenario: Role 取得多個 Permissions
- **WHEN** 管理者將多個有效 Permission keys 指派給現存自訂 Role
- **THEN** 系統保存該 Role 與各 Permission 的唯一 mappings

#### Scenario: Role 被安全刪除
- **WHEN** 未使用的自訂 Role 依 Role Management 規則被刪除
- **THEN** 系統同時移除該 Role 的 Permission mappings
- **THEN** Permission catalog records 保持存在

### Requirement: 授權管理者可查詢 Permission 與 Role mappings
API SHALL 提供受 `permissions.manage` 保護的 Permission catalog 與 Role Permission 查詢 endpoints，回傳穩定 key、顯示 metadata、分類及指定 Role 目前的有效 Permission keys，且 MUST 使用 shared response envelope。

#### Scenario: 有權限者查詢 catalog
- **WHEN** 具有 `permissions.manage` 的 authenticated user 查詢 Permission catalog
- **THEN** API 回傳完整固定 catalog 與可供管理 UI 分組的 metadata

#### Scenario: 無權限者查詢 catalog
- **WHEN** authenticated user 不具有 `permissions.manage` 且查詢 Permission catalog
- **THEN** API 回傳 HTTP 403 shared failure envelope

### Requirement: 授權管理者可冪等設定 Role Permissions
API SHALL 允許具有 `permissions.manage` 的 authenticated user 以完整 Permission key 集合設定 `User` 或自訂 Role mappings，MUST 先驗證 Role 與所有 Permission keys，並 MUST 以單一 transaction 完成 mapping 差異更新。

#### Scenario: 設定有效 Permission 集合
- **WHEN** 有權限者替現存自訂 Role 提交有效且不同於現況的完整 Permission 集合
- **THEN** 系統以單一 transaction 新增與移除所需 mappings
- **THEN** API 回傳更新後的完整有效 Permission 集合

#### Scenario: 重複提交相同集合
- **WHEN** 有權限者提交與 Role 現況相同的 Permission 集合
- **THEN** API 回傳成功且不建立重複 mappings

#### Scenario: 提交未知 Permission
- **WHEN** request 包含不在目前 catalog 的 Permission key
- **THEN** API 回傳 HTTP 400 shared validation failure
- **THEN** Role 原有 mappings 保持不變

#### Scenario: 設定不存在 Role
- **WHEN** request 指定不存在的 Role
- **THEN** API 回傳 HTTP 404 shared failure envelope
- **THEN** 不變更任何 mappings

### Requirement: System Admin 永遠具有完整 Permissions
系統 MUST 將 `Admin` 視為不可削減 Permission 的 system role，且任何具有有效 `Admin` role 的 principal MUST 取得目前 catalog 的全部 Permissions。

#### Scenario: Admin 取得完整 catalog
- **WHEN** 系統為具有 `Admin` role 的啟用中帳號建立 access token
- **THEN** token 的有效 Permission 集合包含目前 catalog 的每一個 key

#### Scenario: 嘗試削減 Admin Permissions
- **WHEN** 管理 request 對 `Admin` role 提交少於完整 catalog 的 Permission 集合
- **THEN** API 回傳 HTTP 409 shared failure envelope
- **THEN** Admin 的有效 Permissions 保持完整

### Requirement: Role Permission 異動撤銷受影響 Sessions
Role Permission mapping 成功改變後，系統 MUST 更新所有持有該 Role 帳號的 security stamp，並 MUST 撤銷其 Memory access sessions 與 persisted refresh families，使舊 Permission claims 不再可用。

#### Scenario: 使用中 Role 的 Permissions 改變
- **WHEN** 一個已指派帳號的 Role Permission 集合成功改變
- **THEN** 所有持有該 Role 帳號的既有 access sessions 與 refresh families 被撤銷
- **THEN** 其舊 JWT 在下一次受保護 request 被拒絕

#### Scenario: Mapping transaction 失敗
- **WHEN** mapping persistence 或任一必要 security stamp 更新失敗
- **THEN** API 回傳 failure response
- **THEN** mappings 與受影響帳號 security state 保持 mutation 前狀態

### Requirement: 前端提供 Role Permission 管理操作面
前端 SHALL 使用 PrimeVue 在 Role Management 提供僅對具有 `permissions.manage` 的使用者顯示的 Permission 設定入口，依 catalog metadata 分組顯示並提交指定 Role 的完整 Permission 集合。

#### Scenario: 管理自訂 Role Permissions
- **WHEN** 有權限使用者開啟自訂 Role 的 Permission 設定
- **THEN** 前端載入 catalog 與該 Role 目前 mappings
- **THEN** 使用者可選擇 Permissions 並確認提交完整集合

#### Scenario: 顯示 Admin Permissions
- **WHEN** 有權限使用者查看 system `Admin` role
- **THEN** 前端顯示完整 Permissions 且將削減操作設為 disabled
- **THEN** 畫面清楚說明 Admin 是不可削減的 system role

#### Scenario: 無管理權限的使用者
- **WHEN** authenticated user 不具有 `permissions.manage`
- **THEN** 前端不顯示 Permission 管理入口
- **THEN** 後端仍對直接呼叫管理 API 回傳 HTTP 403

