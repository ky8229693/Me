## Context

第一階段 `jwt-memory-session-allowlist` 將 access JWT 改成單機 authoritative allowlist：每顆 token 有唯一 `jti`，protected request 不查 PostgreSQL，security mutation 會撤銷 user 全部 sessions。代價是 access token 仍維持 60 分鐘，且 API restart 清空 Memory 後，尚未到期的 token 也無法恢復，使用者必須重新輸入 credentials。

第二階段需要讓 access token 更短效，並用 server-side、可撤銷的 refresh credential 恢復 session。Frontend 是 Vue SPA，目前 access token 保存於 `localStorage`；backend 使用 ASP.NET Core Identity、EF Core 與 PostgreSQL，local development 維持 HTTP only、單一 API instance。

## Goals / Non-Goals

**Goals:**

- Access token 預設縮短為 15 分鐘，並維持 Memory `jti` allowlist 驗證。
- Login／register 建立固定 7 天 absolute lifetime 的 refresh-token family。
- Refresh token 每次使用後 rotation，舊 token replay 會撤銷整個 family。
- Refresh token 原文只存在 HttpOnly cookie，database 只保存 SHA-256 hash。
- Refresh 時重新查詢目前 Identity user、enabled state 與 roles，再發行新 access token。
- Security mutation 同時撤銷 Memory access sessions 與 PostgreSQL refresh families。
- Frontend 使用 memory-only access token、startup recovery 與 single-flight refresh/retry。
- API restart 後可用仍有效的 refresh cookie 建立新的 Memory access session。

**Non-Goals:**

- Redis、distributed Memory allowlist 或多 API instance 支援。
- OAuth authorization code、OpenID Connect、外部 identity provider 或 DPoP。
- Remember-me、多種 refresh lifetime、裝置/session 管理 UI 或 user-selective remote logout。
- 多瀏覽器分頁之間的 refresh coordination；single-flight 只保證同一個 JavaScript runtime。
- 將 refresh token 放入 JSON response、localStorage 或 sessionStorage。

## Decisions

### 1. Opaque refresh token 與 hash-only persistence

Refresh token 使用 cryptographically secure random 32-byte value並以 Base64Url 編碼。Server 只在 cookie response 中傳送原文，PostgreSQL 保存 SHA-256 hash；應用程式 logs、exceptions、API response body 與 telemetry MUST NOT 包含 token 或 cookie value。

新增 refresh token entity，每次 rotation 建立一列，至少包含：token id、token hash、family id、user id、created time、absolute expiry、consumed time、revoked time、replacement token id 與 revoke reason。`TokenHash` 建立 unique index，`FamilyId`、`UserId` 與 expiration 建立查詢 indexes。

高熵 opaque token 讓單向 SHA-256 hash 足以防止 database 內容直接成為 bearer credential。替代方案是可自我驗證的 JWT refresh token，但它會讓 rotation/revocation仍依賴 server state，且更容易誤把 refresh claims 當授權依據，因此不採用。

### 2. Refresh family 固定 7 天 absolute expiry

Login／register 建立新的 `sid` family id 與第一顆 refresh token。每次 rotation 沿用 family id 與原始 absolute expiry，不延長 7 天上限；access token預設 15 分鐘。兩個 lifetime 都從 `Jwt` configuration 讀取。

固定 family expiry 可避免持續使用讓 session 永久存在，也讓 cleanup 與安全邊界容易推理。本 change 不提供 remember-me 或 sliding refresh expiry。

### 3. HttpOnly、SameSite Strict、path-limited cookie

Refresh token cookie 使用 `HttpOnly=true`、`SameSite=Strict`、`Path=/api/auth`。非 Development 環境 MUST 設定 `Secure=true`；HTTP-only Development 使用 `Secure=false`，且此例外不得由 production configuration 關閉。

Refresh／logout requests 必須使用 relative `/api` path並包含 credentials。當 request 帶有 `Origin` header 時，backend 只接受 configured CORS allowed origins；搭配 SameSite Strict 降低 CSRF 風險。Cookie name 與 refresh lifetime由 configuration 提供，response 不回傳 refresh token 原文。

### 4. Rotation 在 serializable transaction 中單次消耗

`POST /api/auth/refresh` 讀取 cookie、計算 hash，在 serializable transaction 中鎖定／重新確認 token row。只有未 consumed、未 revoked、未 expired 的 token 可以成功一次：

1. 查詢 user 並拒絕不存在或 disabled account。
2. 將目前 row 標記 consumed。
3. 建立同 family、相同 absolute expiry 的 replacement row。
4. Commit transaction。
5. 撤銷該 family 的舊 Memory access sessions。
6. 依目前 roles 發行含新 `jti` 與相同 `sid` 的 15 分鐘 access token。
7. 設定 replacement refresh cookie並回傳 `AuthResponse`。

Concurrent refresh 中只能有一個成功。已 consumed token 再次出現視為 replay：在 transaction 中撤銷 family 所有 refresh rows，commit 後撤銷該 family 的 Memory access sessions、清除 cookie並回傳 401。這可能把極少數同時 refresh 誤判為 replay；同 runtime single-flight 是主要緩解，多分頁協調留給後續 change。

### 5. Access session registry 加入 family index

Access JWT 新增 `sid` claim；Memory session entry 保存 `jti`、user id、family id 與 expiry。Registry 除了 user-wide revocation，再提供 family-wide revocation及 `familyId -> jti set` index。

每次 refresh 成功時先撤銷同 family 的舊 access sessions，再註冊新的 access token，讓同 family 只有最新 access generation 可使用。User-wide security mutation 仍撤銷該 user 所有 family access sessions。

### 6. Logout 以 refresh family 為主要撤銷單位

`POST /api/auth/logout` 不要求有效 access token；它從 refresh cookie hash 找到 family並撤銷 family refresh rows及 Memory access sessions，之後一律清除 cookie並回傳成功 shared envelope。Unknown、expired、已撤銷 cookie與未帶 cookie都保持冪等，不洩漏 token 是否存在。

若 request 同時提供 active bearer token，可利用 `sid` 加速 family revocation，但 database refresh family仍是 authoritative logout state。這確保 access token 已到期時仍能正確登出 refresh session。

### 7. Security mutation 使用集中式 session revocation service

建立 user session revocation abstraction，協調：

- `IJwtSessionRegistry.RevokeAll(userId)`。
- 將該 user 未到期 refresh families 標記 revoked。

Account enable/disable、role assignment、Role rename、Admin bootstrap promotion與其他 security-stamp mutation成功後、回應成功前使用相同 abstraction。Database mutation與 refresh revocation應在可行時共用 transaction；Memory revocation在 commit 後、success response前執行。No-op或rollback不得撤銷 sessions。

### 8. Frontend access token 改為 memory-only

`authToken` module 改用 module-scoped variable，不再讀寫 `localStorage`；啟動時主動刪除舊 `web_demo.auth_token` key，避免 migration 後殘留 credential。

App startup 呼叫 refresh endpoint：成功則保存新 access token與 user；失敗則保持未登入。Login／register 只保存 response access token到 memory，refresh cookie由 browser管理。

### 9. Shared API client single-flight refresh與一次 retry

Shared HTTP client遇到非 auth endpoint的 401時，共用同一個 in-flight refresh Promise。同一 runtime 的其他 requests等待結果；成功後以新 access token retry原 request一次。以下情況不得觸發 refresh：

- Login、register、refresh或logout request。
- 已 retry過的 request。
- Request 明確禁用 authentication。

Refresh失敗時清除 memory token並通知 auth state失效；禁止 recursive refresh或無限 retry。Business 403、400、409與network error不得觸發 refresh。

## Risks / Trade-offs

- [Refresh token database breach] → 只保存高熵 token SHA-256 hash，不保存原文，不寫入 logs。
- [Refresh cookie theft] → HttpOnly、SameSite Strict、production Secure、7 天 absolute expiry、每次 rotation與reuse detection。
- [Concurrent refresh 被視為 replay] → 同 runtime single-flight；server transaction確保安全優先。多分頁協調列為後續工作。
- [API restart後 access token失效] → Startup或下一次 401可用 persisted refresh cookie取得新 access session。
- [Access token memory-only造成頁面 reload短暫 loading] → App先完成 session restore再決定顯示 Login或主畫面。
- [Mutation漏撤銷 refresh families] → 集中 session revocation service並為每個 mutation path建立 regression tests。
- [Cookie在 HTTP development不能使用 Secure] → 僅 Development允許 `Secure=false`；production強制 Secure且仍不主動導入本機 HTTPS。
- [Database rows持續成長] → 提供 background/command cleanup，刪除超過 retention 的 expired/revoked rows；cleanup不影響active family。

## Migration Plan

1. 先完成並同步 `jwt-memory-session-allowlist`；本 change 不可獨立部署。
2. 新增 refresh-token migration並部署 database schema。
3. 同一版本部署 backend refresh endpoint、cookie、family-aware registry與 frontend memory-only recovery。
4. 部署時刪除 frontend舊 localStorage token；使用者需重新登入一次取得第一顆 refresh cookie。
5. Rollback時停止使用 refresh endpoint、清除 refresh cookie並恢復60分鐘 access-token-only flow；database table可暫時保留，後續 migration再移除。

## Open Questions

- 無；本階段固定採15分鐘 access token、7天 absolute refresh family與單一 JavaScript runtime single-flight。
