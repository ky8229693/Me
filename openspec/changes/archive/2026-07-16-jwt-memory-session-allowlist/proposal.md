## Why

目前每個 authenticated request 都會查詢 PostgreSQL，以確認 Identity user、停用狀態與 security stamp，讓固定的資料庫 lookup 成為所有受保護 API 的成本。第一階段改用單機 Memory 維護有效 JWT sessions，使 request 可在完成 JWT 密碼學驗證後，以記憶體中的 session allowlist 決定是否接受 token，同時保留帳號與角色異動後立即撤銷舊權限的行為。

## What Changes

- 每個新 JWT 加入唯一、不可預測的 `jti`，並在發行成功時以相同到期時間註冊到 Memory session allowlist。
- 每個 authenticated request 先驗證 JWT signature、issuer、audience 與 expiration，再確認 `jti` 仍存在於 allowlist；不再於每次 request 查詢 Identity user 或比對資料庫 security stamp。
- 維護 `jti` 到 user 與 user 到 active sessions 的索引，讓帳號狀態、角色指派、Role rename 或其他 security stamp 異動可撤銷該使用者所有 sessions。
- Logout 改為撤銷目前 JWT session，而不只清除前端 local state。
- Memory miss、entry 到期、eviction 或 API restart 後遺失的 session 一律回傳 unauthorized，採 fail-closed 行為。
- 保留目前 60 分鐘 access-token lifetime；本 change 不加入 refresh token、自動續期、Redis 或多 API instance 支援。
- **BREAKING**：部署或 API restart 會清空 allowlist，所有既有 JWT 即使尚未到期也必須重新登入。

## Capabilities

### New Capabilities
- `jwt-session-allowlist`: 定義單機 Memory JWT session 註冊、request 驗證、依使用者撤銷、到期與 restart fail-closed 行為。

### Modified Capabilities
- `auth-foundation`: JWT 發行與驗證改以 `jti` session allowlist 判斷有效性，帳號／角色安全異動撤銷 sessions，logout 需通知後端撤銷目前 session。

## Impact

- Backend：JWT token service、bearer validation event、auth endpoints、帳號與角色管理 services，以及新的 singleton in-memory session registry。
- Frontend：logout API helper 需呼叫受保護的 backend logout endpoint，再清除 local token；現有 bearer-token 儲存方式與登入 response contract維持不變。
- Tests：新增 session issuance、allowlist validation、expiration、restart/miss、logout 與 user-wide revocation 測試，並更新帳號及角色異動 regression tests。
- Operations：此階段只支援單一 API instance；memory pressure 或 process restart 可能讓使用者提早登出，但不得讓已撤銷或未知 session 通過驗證。
