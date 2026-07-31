## Context

目前 API 發行 60 分鐘 JWT，token 內包含 user id、email、roles 與 security stamp。`JwtBearerEvents.OnTokenValidated` 會在每個 authenticated request 透過 `UserManager` 查詢 PostgreSQL，再確認 user 存在、未停用且 security stamp 相符。這提供即時撤銷能力，但讓所有受保護 API 都產生固定的 Identity user lookup。

本 change 將 JWT 改成「密碼學驗證 + server-side session allowlist」模式。JWT 仍承載 identity 與 role claims，但只有在單機 Memory 中註冊且未被撤銷的 `jti` 才是有效 session。專案目前是單一 API instance、HTTP-only local development，且沒有 refresh-token lifecycle。

## Goals / Non-Goals

**Goals:**

- 移除一般 authenticated request 的 Identity/PostgreSQL user lookup。
- 保留 JWT signature、issuer、audience、expiration 等標準驗證。
- 讓帳號停用、啟用、角色指派、Role rename 與 security stamp 異動後，舊 sessions 在下一個 protected request 被拒絕。
- 支援同一 user 多個 sessions、目前 session logout 與 user-wide revocation。
- Memory miss、eviction 或 process restart 時採 fail-closed。

**Non-Goals:**

- Refresh token、自動續期或 silent re-authentication。
- Redis、distributed cache、sticky session 或多 API instance 一致性。
- 裝置管理 UI、列出 sessions、選擇性遠端登出或 session metadata 稽核。
- 改變 60 分鐘 access-token lifetime、JWT signing algorithm 或前端 localStorage 策略。
- 移除 Identity security stamp 本身；現有帳號／角色 mutation 仍可更新 stamp，但 request validation 不再查詢它。

## Decisions

### 1. 使用 singleton session registry，而不是快取 Identity user

建立 `IJwtSessionRegistry` singleton abstraction，至少提供：註冊 session、確認 `jti` 與 `userId` 是否 active、撤銷目前 `jti`、撤銷 user 全部 sessions。實作以 `IMemoryCache` 保存 `jti` session entry，並維護 concurrency-safe 的 `userId -> jti set` 反向索引。

這個元件是有效 session allowlist，不是可任意 miss 後回源的資料 cache。若 entry 不存在，request 必須直接 unauthorized，不能查 DB 後重新建立，否則已撤銷 token 可能復活。

替代方案是 cache user security stamp，cache miss 時查 DB；該方案仍保留 cold-miss DB 路徑且 cache 語意較複雜，不符合本階段「只接受已註冊 token」的決策。

### 2. JWT 使用 `jti` 作為 session identity

每次 login 或 register 發行 JWT 時產生高熵唯一 `jti` claim，registry entry 保存 `jti`、`userId` 與 expiration。Token 只有在 registry 註冊成功後才能回傳給 client；entry 使用與 JWT 相同的 absolute expiration，不延長 session lifetime。

Registry 不以完整 bearer token 當 key，也不保存 password、role claims 或其他 Identity user snapshot。驗證時同時比對 token 的 `jti` 與 `userId`，避免錯誤索引或碰撞造成跨 user session 接受。

### 3. 標準 JWT 驗證先於 allowlist 驗證

保留 JwtBearer middleware 對 signature、issuer、audience、lifetime 與 signing key 的驗證。只有密碼學驗證成功後，`OnTokenValidated` 才讀取 `jti` 與 name identifier，並查詢 registry。缺少 claim、entry 不存在、user 不符或已到期時呼叫 `context.Fail`。

一般 request 不注入 `UserManager`、不查詢 user 狀態，也不比對 security stamp。Identity 與 role claims 以 token 發行時的 snapshot 為準，直到 session 被 mutation flow 撤銷。

### 4. 安全相關 mutation 完成後、回應成功前撤銷 user sessions

所有會改變 token identity 或 authorization 結果的成功流程，必須在回傳 success 前呼叫 user-wide revocation：

- 帳號停用或啟用。
- 完整 role assignment 異動。
- Role rename 影響到的每一位 user。
- Admin bootstrap 對既有 user 的角色提升。
- 其他會更新 security stamp 的新流程。

資料庫 transaction 先完成，再執行同步 in-memory revocation，最後才回傳成功。這與現有「mutation 成功後下一個 protected request 失效」語意一致；mutation transaction 執行中的 concurrent request 與目前資料庫查詢方案同樣可能仍看到舊狀態。

若 mutation 判定為 no-op，不必撤銷 sessions；若資料庫 mutation 失敗或 rollback，也不得撤銷。Role rename 必須在 transaction 內收集受影響 user ids，commit 後逐一撤銷。

### 5. Logout 撤銷目前 session，local cleanup 永遠執行

`POST /api/auth/logout` 改為需要 authentication，從 current principal 取得 `jti` 並撤銷該 session。它不撤銷同 user 的其他登入；user-wide logout 不在本 change。

前端 logout helper 改成非同步呼叫 backend；無論 request 成功、401 或網路失敗，都必須在 finally 清除 local access token，避免 client 卡在已登出狀態。因為沒有 refresh token，登出後只能重新登入。

### 6. Memory lifecycle 採 fail-closed

Entry 到期或 capacity eviction 時，eviction callback 必須清理 user 反向索引。API restart、deployment 或 registry reset 會失去全部 entries，因此舊 JWT 即使簽章與 expiration 仍有效，也會收到 401。

這是第一階段刻意接受的單機限制。未來多 instance change 應將 registry 改成 Redis 等共享 authoritative session store，而不是讓 Memory miss 回源並重新接受未知 token。

## Risks / Trade-offs

- [API restart 讓所有使用者登出] → 在 proposal、規格與營運文件明確記錄；第二階段 refresh-token 或 distributed registry 再改善體驗。
- [Memory pressure eviction 造成提早登出] → 設定合理 cache size／entry size，監控 eviction；安全上維持 fail-closed。
- [漏接新的 user／role mutation 路徑會保留舊權限] → 集中 user-wide revocation abstraction，為每個既有 mutation 與未來 security-stamp mutation 建立 regression tests，並記錄開發規範。
- [Registry 與反向索引 concurrency race] → 以 thread-safe collections 與 per-user synchronization 保證 register、revoke-one、revoke-all、expiration cleanup 的一致性。
- [JWT 從 stateless 變成 stateful] → 明確接受，以換取零一般 request DB lookup 與 server-side immediate revocation；registry 不可被當成 optional cache bypass。
- [Logout request 已失效或網路中斷] → 前端仍清除 local token；server entry 最晚於 JWT expiration 自動消失。

## Migration Plan

1. 部署 registry、`jti` issuance 與 validation 必須同一版本完成。
2. 部署後既有 JWT 沒有 registry entry（且可能沒有 `jti`），第一次 protected request 將收到 401，使用者重新登入後取得新 session。
3. 若需 rollback，恢復原本 security-stamp DB validation；新 JWT 可保留 `jti` 而不影響舊 validator。
4. 更新 `docs/memory.md`，記錄單機 allowlist、restart logout 與 mutation revocation 邊界。

## Open Questions

- 無；第一階段已決定接受單機 Memory 與 restart 全部登出的限制。
