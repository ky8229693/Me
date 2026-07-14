## ADDED Requirements

### Requirement: 第一位 Admin 可透過受控命令建立
系統 SHALL 提供非 HTTP 的 bootstrap command，在未存在 Admin 帳號時建立新的 Identity user 並配置 `Admin` 與 `User` roles，且 MUST NOT 內建預設帳密。

#### Scenario: 建立新的第一位 Admin
- **WHEN** 操作者在受控環境執行 bootstrap command，提供有效且尚未註冊的 Email 與符合規則的密碼，且系統尚無 Admin
- **THEN** 系統建立 Identity user 並配置 `Admin` 與 `User` roles
- **THEN** command 以成功狀態結束且不輸出密碼

#### Scenario: Bootstrap 輸入無效
- **WHEN** bootstrap command 收到無效 Email、弱密碼或缺少必要輸入
- **THEN** command MUST 以失敗狀態結束並提供不含敏感資料的錯誤訊息
- **THEN** 系統不建立部分完成的 Admin 帳號

### Requirement: Bootstrap 可安全處理既有帳號與重複執行
Bootstrap command MUST 在變更資料前確認目前 Admin 狀態，並 SHALL 支援將指定的既有一般帳號提升為第一位 Admin。

#### Scenario: 提升既有帳號為第一位 Admin
- **WHEN** 指定 Email 已對應啟用中的 Identity user、系統尚無 Admin，且操作者確認提升
- **THEN** 系統為該使用者配置 `Admin` role並保留 `User` role
- **THEN** 系統更新使用者 security stamp

#### Scenario: 系統已存在 Admin
- **WHEN** 系統已有至少一位 Admin 且 bootstrap command 再次執行
- **THEN** command MUST 拒絕建立或提升其他 Admin
- **THEN** 既有帳號與角色資料保持不變

### Requirement: Bootstrap 不暴露遠端匿名入口
API MUST NOT 提供允許匿名 request 建立、提升或配置 Admin 的 bootstrap endpoint。

#### Scenario: 檢查公開 API routes
- **WHEN** API routes 完成註冊
- **THEN** 系統不存在匿名 Admin bootstrap route
