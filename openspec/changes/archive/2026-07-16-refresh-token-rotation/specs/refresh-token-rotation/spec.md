## ADDED Requirements

### Requirement: Refresh token 使用安全 cookie 與 hash-only persistence
API SHALL 產生高熵 opaque refresh token，MUST 只以 `HttpOnly`、`SameSite=Strict`、`Path=/api/auth` cookie傳送原文，並 MUST 只在PostgreSQL保存不可逆 token hash與lifecycle metadata。

#### Scenario: Login 建立 Refresh Family
- **WHEN** enabled user成功登入
- **THEN** API建立唯一 refresh family與第一顆refresh token record
- **THEN** response設定HttpOnly refresh cookie但不在JSON body回傳refresh token

#### Scenario: Register 建立 Refresh Family
- **WHEN** 新user成功註冊
- **THEN** API建立唯一 refresh family與第一顆refresh token record
- **THEN** response設定HttpOnly refresh cookie但不在JSON body回傳refresh token

#### Scenario: Database 不保存可用 Credential
- **WHEN** refresh token被持久化
- **THEN** database只保存token hash、family、user、created、expiry、consumed、revoked與replacement metadata
- **THEN** database MUST NOT保存refresh token原文

#### Scenario: Production Cookie Policy
- **WHEN** API在非Development環境設定refresh cookie
- **THEN** cookie MUST同時使用`HttpOnly`、`SameSite=Strict`與`Secure`

#### Scenario: HTTP Development Cookie Policy
- **WHEN** API在HTTP-only Development環境設定refresh cookie
- **THEN** cookie維持`HttpOnly`與`SameSite=Strict`但使用`Secure=false`

### Requirement: Refresh token 每次成功使用都 Rotation
`POST /api/auth/refresh` SHALL在serializable transaction中單次消耗有效refresh token、建立同family replacement token，並回傳依目前user與roles發行的新access token。

#### Scenario: 有效 Refresh Token
- **WHEN** request帶有未過期、未consumed且未revoked的refresh cookie
- **THEN** API將舊token標記consumed並建立replacement token
- **THEN** replacement沿用原family id與原family absolute expiry
- **THEN** API設定replacement cookie並回傳新的15分鐘access token與目前user資料

#### Scenario: Concurrent Refresh
- **WHEN** 多個requests同時提交相同refresh token
- **THEN** transaction與concurrency control MUST確保最多一個request成功rotation
- **THEN** 其他request依reuse規則處理且不得建立第二個active replacement

#### Scenario: Refresh Family 到期
- **WHEN** refresh family超過可設定的預設7天absolute expiry
- **THEN** API拒絕refresh並回傳HTTP 401 shared unauthorized response
- **THEN** API清除refresh cookie且不得延長family expiry

#### Scenario: User 已停用或不存在
- **WHEN** refresh token對應user不存在或目前disabled
- **THEN** API撤銷該family、清除cookie並回傳HTTP 401

### Requirement: Refresh Token Reuse 撤銷整個 Family
API MUST將已consumed refresh token再次出現視為可能的credential replay，並 MUST撤銷該family全部refresh tokens與Memory access sessions。

#### Scenario: Consumed Token 被重用
- **WHEN** request提交已成功rotation且標記consumed的refresh token
- **THEN** API撤銷同family所有refresh records
- **THEN** API撤銷同family全部Memory access sessions
- **THEN** API清除cookie並回傳HTTP 401

#### Scenario: Revoked Token 被提交
- **WHEN** request提交已revoked refresh token
- **THEN** API不得發行任何token並回傳HTTP 401

### Requirement: Access Token 與 Refresh Family 綁定
每顆access JWT SHALL包含唯一`jti`與refresh family `sid`，Memory session registry MUST支援family-wide revocation，且refresh成功後 MUST使同family舊access sessions失效。

#### Scenario: Refresh 發行新 Access Generation
- **WHEN** refresh rotation成功
- **THEN** API撤銷同`sid`目前所有Memory access sessions
- **THEN** API註冊包含新`jti`及相同`sid`的新access token

#### Scenario: API Restart 後恢復 Session
- **WHEN** API restart清空Memory allowlist但browser仍有有效refresh cookie
- **THEN** frontend可呼叫refresh endpoint取得新的registered access session
- **THEN** user不需要重新提交credentials

### Requirement: Logout 冪等撤銷 Refresh Family
`POST /api/auth/logout` SHALL以refresh cookie識別並撤銷目前family、撤銷其Memory access sessions且清除cookie，即使access token已失效；missing或unknown cookie MUST保持冪等成功。

#### Scenario: 有效 Refresh Session 登出
- **WHEN** client帶有效refresh cookie呼叫logout
- **THEN** API撤銷該family全部refresh與access sessions
- **THEN** API清除refresh cookie並回傳shared success response

#### Scenario: Access Token 已到期仍可登出
- **WHEN** client的access token已到期但refresh cookie仍有效
- **THEN** logout仍撤銷refresh family並清除cookie

#### Scenario: Missing 或 Unknown Cookie 登出
- **WHEN** logout request未帶refresh cookie或cookie無法對應token record
- **THEN** API清除cookie並回傳shared success response
- **THEN** response MUST NOT洩漏refresh token是否曾存在

### Requirement: Security Mutation 同時撤銷 Access 與 Refresh Sessions
帳號可用性、roles、role names或其他security-sensitive user state成功異動後，系統 MUST在回傳success前撤銷受影響user全部Memory access sessions與database refresh families。

#### Scenario: Account 或 Role 異動
- **WHEN** Admin成功停用、啟用或變更user roles
- **THEN** 系統撤銷該user全部access sessions與refresh families
- **THEN** 舊access與refresh token皆不得再取得authenticated response

#### Scenario: Role Rename 影響多位 Users
- **WHEN** Admin成功rename已指派給多位users的role
- **THEN** 系統對每位受影響user撤銷全部access sessions與refresh families

#### Scenario: Mutation 失敗或 No-op
- **WHEN** mutation rollback、失敗或未產生實際security state變更
- **THEN** 系統不因該request撤銷既有sessions

### Requirement: Frontend Single-flight 自動續期
Frontend shared API client SHALL在符合條件的401 response後，以single-flight方式執行一次refresh並最多retry原request一次；refresh失敗時 MUST清除memory auth state且不得循環retry。

#### Scenario: 單一 Request Access Token 到期
- **WHEN** 非auth API因access token失效回傳401且refresh cookie有效
- **THEN** frontend呼叫refresh endpoint、保存新access token並retry原request一次

#### Scenario: 多個 Requests 同時收到401
- **WHEN** 同一JavaScript runtime中多個requests同時需要refresh
- **THEN** frontend只送出一個refresh request
- **THEN** 其他requests等待同一結果並使用新token各retry一次

#### Scenario: Refresh 失敗
- **WHEN** refresh endpoint回傳401或network failure
- **THEN** frontend清除memory access token並切換為未登入狀態
- **THEN** frontend不得對refresh、logout或已retry request再次觸發refresh

### Requirement: Refresh Lifecycle 具有清理與測試保障
系統 SHALL提供清理expired／revoked refresh records的安全機制，並 SHALL以backend、frontend與E2E tests覆蓋rotation、reuse、restart recovery、mutation revocation與logout。

#### Scenario: 清理歷史 Records
- **WHEN** cleanup執行
- **THEN** 系統只刪除超過retention的expired或revoked refresh records
- **THEN** active refresh family保持可用

#### Scenario: 完整驗證
- **WHEN** automated tests執行
- **THEN** tests涵蓋refresh rotation、concurrent reuse、family revocation、account／role mutation、memory-only token、single-flight retry與logout
