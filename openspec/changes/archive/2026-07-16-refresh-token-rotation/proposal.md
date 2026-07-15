## Why

第一階段 Memory JWT session allowlist 移除了每個 authenticated request 的 PostgreSQL lookup，但 access token 到期或 API restart 後仍會要求使用者重新登入。第二階段加入可撤銷、可偵測重放的 refresh-token rotation，讓 access token 可以縮短並自動續期，同時維持帳號與角色異動後立即失效的安全邊界。

## What Changes

- 此 change 依賴 `jwt-memory-session-allowlist` 完成、同步並保留其 `jti` authoritative allowlist 行為。
- Access token lifetime 從 60 分鐘縮短為可設定的 15 分鐘，JWT 增加 session family `sid` claim；每次 refresh 發行新的 access-token `jti`。
- Login／register 成功時建立 refresh-token family，產生高熵 opaque refresh token，以 `HttpOnly`、`SameSite=Strict` cookie 傳送，資料庫只保存 token hash 與 lifecycle metadata。
- 新增 `POST /api/auth/refresh`，每次成功使用 refresh token 都 rotation：舊 token 標記 consumed、建立 replacement token、更新 cookie，並撤銷同 family 的舊 access sessions。
- 偵測已 consumed refresh token 被重用時，撤銷整個 family 與相關 access sessions；refresh token 過期、撤銷、user 不存在或停用時一律拒絕。
- Logout 改為以 refresh cookie 撤銷目前 family、清除 cookie，並保持冪等；帳號／角色 security mutation 撤銷該 user 的所有 refresh families 與 Memory access sessions。
- Frontend access token 改為 memory-only，不再保存到 `localStorage`；頁面啟動時使用 refresh cookie 恢復 session。
- Shared API client 在非 auth request 收到 401 時，使用 single-flight refresh，成功後僅 retry 原 request 一次；refresh 失敗則清除 auth state，不進行循環 retry。
- API restart 後 Memory access sessions 會消失，但有效 refresh cookie 可取得新的 access token，不必重新輸入 credentials。
- **BREAKING**：既有 `localStorage` access token 不再作為跨 reload session persistence；access token lifetime 預設縮短為 15 分鐘，client 必須支援 cookie refresh flow 才能維持登入。

## Capabilities

### New Capabilities
- `refresh-token-rotation`: 定義 refresh-token family、hash persistence、rotation、reuse detection、cookie、refresh endpoint、family revocation 與前端 single-flight renewal。

### Modified Capabilities
- `auth-foundation`: 認證 token lifecycle 改為短效 access token、memory-only frontend token、refresh cookie session recovery，以及 family-aware logout 與 security mutation revocation。

## Impact

- Backend data：新增 refresh-token persistence entity、EF Core mapping、indexes 與 PostgreSQL migration。
- Backend services：token issuance、session registry family indexing、refresh rotation transaction、logout、帳號／角色 mutation revocation 與 cookie policy。
- API：新增 `/api/auth/refresh`；login/register/logout response 仍使用 shared envelope，但會設定或清除 refresh cookie。
- Frontend：auth token storage、startup bootstrap、shared HTTP retry coordination、logout 與 auth state tests。
- Security：refresh token 原文只存在 cookie/client；database breach 不得直接取得可用 token。Production cookie 必須 `Secure`，HTTP-only local development 使用環境限定設定。
- Operations：仍維持單一 API instance；Redis/distributed access-session registry、多裝置 session UI 與 remember-me 選項不在本 change。
