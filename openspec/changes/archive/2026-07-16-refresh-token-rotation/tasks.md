## 1. 前置依賴、設定與資料模型

- [x] 1.1 確認 `jwt-memory-session-allowlist` 已完成並將其 delta specs 同步到主規格，保留 `jti` fail-closed 行為
- [x] 1.2 擴充 JWT options，加入預設15分鐘 access lifetime、7天 absolute refresh lifetime、cookie name與production Secure policy驗證
- [x] 1.3 建立 refresh token entity與EF Core mapping，包含hash、family、user、created/expiry、consumed/revoked、replacement與reason欄位
- [x] 1.4 新增unique token-hash index與family、user、expiry查詢indexes，產生並檢視PostgreSQL migration
- [x] 1.5 新增configuration與model tests，確認Development／production cookie policy及migration schema

## 2. Refresh Token Crypto、Persistence 與 Rotation Service

- [x] 2.1 建立secure random 32-byte Base64Url refresh token generator與SHA-256 hash utility，確保原文不進入entity、logs或exceptions
- [x] 2.2 建立refresh family issuance service，於login/register建立首顆token record並回傳只供cookie使用的原文
- [x] 2.3 實作serializable refresh rotation transaction，單次consume舊token、建立同family replacement並維持原absolute expiry
- [x] 2.4 實作expired、revoked、missing、user不存在與disabled user的401結果及cookie清除指示
- [x] 2.5 實作consumed-token reuse detection，撤銷整個family且禁止concurrent request建立第二個active replacement
- [x] 2.6 建立refresh record cleanup service或command，只清除超過retention的expired/revoked records
- [x] 2.7 新增service tests，涵蓋hash-only persistence、rotation、concurrency、absolute expiry、reuse、user狀態與cleanup

## 3. Family-aware Access Session 與 JWT Issuance

- [x] 3.1 擴充Memory session entry與`IJwtSessionRegistry`，保存`sid`並提供family-wide revoke及family-to-jti reverse index
- [x] 3.2 更新expiration／eviction／revoke-one／revoke-user清理邏輯，確保user與family兩份反向索引一致
- [x] 3.3 修改JWT issuance，加入`sid`與新`jti`、使用15分鐘lifetime並在指定family註冊Memory access session
- [x] 3.4 修改bearer validation，同時要求`jti`、`sid`與user id符合active Memory entry
- [x] 3.5 新增registry與JWT tests，涵蓋family revoke、refresh generation replacement、user mismatch、restart recovery前的401與無Identity request lookup

## 4. Auth Endpoints、Cookie 與 Session Lifecycle

- [x] 4.1 建立集中refresh cookie writer/clearer，使用HttpOnly、SameSite Strict、`/api/auth` path與environment Secure規則
- [x] 4.2 修改register/login，在成功response前建立refresh family、設定cookie並發行相同`sid`的access token
- [x] 4.3 新增`POST /api/auth/refresh`，驗證configured Origin、執行rotation、重查enabled user與current roles、撤銷舊family access generation並回傳新`AuthResponse`
- [x] 4.4 修改logout為不依賴有效access token的冪等family revocation；所有結果清除refresh cookie且不洩漏token存在性
- [x] 4.5 新增auth integration tests，涵蓋cookie flags/body排除、rotation、expired/revoked/reuse、concurrent refresh、restart recovery及expired-access logout

## 5. Security Mutation 統一撤銷

- [x] 5.1 建立user session revocation service，協調database refresh-family revocation與Memory user-wide access revocation
- [x] 5.2 將帳號disable/enable、role assignment與Admin bootstrap promotion改用集中撤銷service
- [x] 5.3 將Role rename成功後的多user revocation改為同時撤銷refresh families與access sessions
- [x] 5.4 確保no-op、validation failure與transaction rollback不撤銷sessions，成功response只在database與Memory revocation完成後回傳
- [x] 5.5 擴充account、role與bootstrap regression tests，確認舊access及refresh token都失效且未受影響user保持登入

## 6. Frontend Memory-only Token 與 Single-flight Refresh

- [x] 6.1 將access token storage改為module memory，啟動時刪除舊`web_demo.auth_token` localStorage且不再寫入persistent storage
- [x] 6.2 擴充auth API client，所有auth requests使用cookie credentials並支援refresh、startup restore與冪等logout
- [x] 6.3 修改shared HTTP client，對eligible 401建立single-flight refresh Promise，成功後最多retry原request一次
- [x] 6.4 防止login/register/refresh/logout、已retry request、403與network errors觸發recursive refresh或無限retry
- [x] 6.5 更新App startup auth state，refresh完成前顯示loading，成功恢復user，失敗才顯示Login
- [x] 6.6 新增frontend unit tests，涵蓋memory-only storage、legacy key cleanup、startup recovery、concurrent 401 single-flight、one retry、refresh failure與logout cleanup

## 7. E2E、文件與完整驗證

- [x] 7.1 擴充Playwright auth E2E，驗證register/login、cookie refresh、access renewal、logout、reload recovery與重新登入
- [x] 7.2 新增backend E2E或integration flow，驗證API restart後舊access 401但有效refresh可建立新Memory session
- [x] 7.3 更新`docs/memory.md`與development文件，記錄15分鐘／7天lifetime、cookie policy、rotation/reuse、單instance與多分頁限制
- [x] 7.4 執行`dotnet test WEB_Demo.sln`、frontend unit tests、production build與完整Playwright E2E
- [x] 7.5 執行OpenSpec strict validation並修正所有錯誤
