## Purpose

定義單一 API process 以 Memory allowlist 管理 JWT sessions、request 驗證、依使用者撤銷與 fail-closed lifecycle 的行為。

## Requirements

### Requirement: JWT sessions 註冊到單機 Memory allowlist
API SHALL 為每個新 access token 產生唯一且不可預測的 `jti`，並 MUST 在回傳 token 前將該 `jti`、user id 與 token expiration 註冊到單機 Memory session allowlist。

#### Scenario: Login 發行已註冊 session
- **WHEN** 啟用中的 user 以有效 credentials 登入
- **THEN** API 發行包含唯一 `jti` 的 JWT
- **THEN** API 在回傳 JWT 前以相同 expiration 註冊 session allowlist entry

#### Scenario: Register 發行已註冊 session
- **WHEN** 新 user 成功完成註冊
- **THEN** API 發行包含唯一 `jti` 的 JWT
- **THEN** API 在回傳 JWT 前以相同 expiration 註冊 session allowlist entry

#### Scenario: Session 到期
- **WHEN** allowlist entry 到達 JWT expiration
- **THEN** registry 移除或視該 entry 為 inactive
- **THEN** registry MUST NOT 以 sliding expiration 延長 JWT session

### Requirement: Protected request 必須具有 active allowlist session
API MUST 先驗證 JWT signature、issuer、audience 與 expiration，再確認 token 的 `jti` 與 user id 對應到 active allowlist entry；一般 allowlist validation MUST NOT 查詢 Identity user 或 PostgreSQL security stamp。

#### Scenario: 已註冊且未到期 JWT
- **WHEN** protected request 提供密碼學驗證成功、未到期且 `jti` 與 user id 對應 active entry 的 JWT
- **THEN** API 接受 token 內的 identity 與 role claims
- **THEN** validation 不執行 Identity user lookup

#### Scenario: 未註冊 JWT
- **WHEN** JWT 的 signature 與 expiration 有效但 `jti` 不存在於 allowlist
- **THEN** API 回傳 HTTP 401 shared unauthorized response
- **THEN** API MUST NOT 從資料庫回填或重新啟用該 session

#### Scenario: Session user 不相符
- **WHEN** JWT 的 `jti` entry 不屬於 token name identifier 所表示的 user
- **THEN** API 回傳 HTTP 401 shared unauthorized response

#### Scenario: JWT 缺少 session claims
- **WHEN** JWT 缺少 `jti` 或 user id claim
- **THEN** API 回傳 HTTP 401 shared unauthorized response

### Requirement: Security mutation 撤銷 user 全部 sessions
系統 MUST 維護 user id 到 active `jti` sessions 的反向索引，並 MUST 在會改變帳號可用性、identity 或 authorization 結果的 mutation 成功後、回傳成功前撤銷受影響 user 的全部 sessions。

#### Scenario: 帳號狀態異動
- **WHEN** Admin 成功停用或重新啟用一個 user
- **THEN** 系統撤銷該 user 的全部 active sessions
- **THEN** 任一舊 JWT 在下一個 protected request 收到 HTTP 401

#### Scenario: User roles 異動
- **WHEN** Admin 成功變更一個 user 的完整 roles 集合
- **THEN** 系統撤銷該 user 的全部 active sessions
- **THEN** 舊 JWT 中的 role claims 不再能通過下一個 protected request

#### Scenario: Role rename 影響多位 Users
- **WHEN** Admin 成功重新命名已指派給一位或多位 users 的自訂 role
- **THEN** 系統撤銷每一位受影響 user 的全部 active sessions

#### Scenario: Mutation 失敗或沒有變更
- **WHEN** security mutation rollback、回傳失敗或判定為 no-op
- **THEN** 系統不因該 request 撤銷既有 sessions

### Requirement: Session lifecycle 採 fail-closed
Memory allowlist SHALL 是單一 API process 內的 authoritative session registry；entry miss、expiration、eviction 或 process restart 後遺失的 session MUST 被視為 inactive。

#### Scenario: API restart
- **WHEN** API process restart 且 Memory allowlist 被清空
- **THEN** restart 前發行的 JWT 即使尚未到期也在下一個 protected request 收到 HTTP 401

#### Scenario: Memory entry 被 eviction
- **WHEN** active JWT 對應的 Memory entry 因 capacity 或其他 eviction 原因消失
- **THEN** API 將該 session 視為 revoked 並回傳 HTTP 401

#### Scenario: 同一 User 多個 Sessions
- **WHEN** 同一 user 由多次 login 或 register flow 取得多個未到期 JWT
- **THEN** registry 分別追蹤每個 `jti`
- **THEN** user-wide revocation 可移除該 user 的全部 sessions
