## 1. Memory Session Registry

- [x] 1.1 建立 `IJwtSessionRegistry` 與 session entry contract，定義 register、is-active、revoke-current、revoke-all-by-user 操作
- [x] 1.2 使用 `IMemoryCache`、thread-safe user-to-jti reverse index 與 absolute expiration 實作 singleton registry
- [x] 1.3 實作 expiration／eviction callback 清理 reverse index，並確保 unknown 或 evicted entry 永遠 fail-closed
- [x] 1.4 新增 registry unit tests，涵蓋多 sessions、user mismatch、revoke-one、revoke-all、expiration、eviction 與 concurrent operations

## 2. JWT Issuance 與 Request Validation

- [x] 2.1 修改 JWT token service，為每次 token 產生唯一高熵 `jti`，並在回傳前以 JWT expiration 註冊 session
- [x] 2.2 修改 bearer `OnTokenValidated`，在標準 JWT 驗證後檢查 `jti`、user id 與 active registry entry，移除每個 request 的 Identity user/security-stamp lookup
- [x] 2.3 對缺少 `jti`、unknown session、user mismatch、expired session 與 restart 後空 registry 統一回傳 shared HTTP 401
- [x] 2.4 新增 JWT issuance 與 protected endpoint integration tests，確認有效 session 通過且 validation 不依賴 Identity user lookup

## 3. Security Mutation Session Revocation

- [x] 3.1 在帳號停用／啟用成功後、回應成功前撤銷目標 user 全部 sessions
- [x] 3.2 在完整 role assignment 實際異動成功後撤銷目標 user 全部 sessions，no-op 或失敗時保留 sessions
- [x] 3.3 在自訂 Role rename transaction 成功後撤銷所有受影響 users 的全部 sessions，rollback 時不得撤銷
- [x] 3.4 在 Admin bootstrap 對既有 user 進行角色提升或其他 security-stamp mutation 時撤銷該 user 全部 sessions
- [x] 3.5 擴充帳號與角色管理 integration tests，確認 mutation 後舊 JWT 下一個 request 401、未受影響 user session 仍有效

## 4. Backend 與 Frontend Logout

- [x] 4.1 將 `POST /api/auth/logout` 改為 authenticated endpoint，從 principal 取得 `jti` 並只撤銷目前 session
- [x] 4.2 修改 frontend auth helper 非同步呼叫 backend logout，並在 success、401 或 network failure 後都清除 local token
- [x] 4.3 更新 App logout flow 與型別，使非同步登出期間 UI 行為一致且不加入 refresh retry
- [x] 4.4 新增 backend logout integration tests 與 frontend unit tests，涵蓋目前 session 撤銷、同 user 其他 session 保留及 local cleanup fallback

## 5. 文件與完整驗證

- [x] 5.1 更新 `docs/memory.md`，記錄 JWT session allowlist、單機限制、restart／eviction 登出與所有 security mutation 必須撤銷 sessions
- [x] 5.2 執行 `dotnet test WEB_Demo.sln`，確認 authentication、account、role 與 regression tests 全部通過
- [x] 5.3 執行 frontend unit tests、production build 與相關 Playwright E2E，確認 login、protected request、logout 與重新登入流程
- [x] 5.4 執行 OpenSpec strict validation 並修正所有錯誤
