## Context

系統目前以 ASP.NET Core Identity 儲存 users 與 roles，JWT 帶有 role claims，管理 API 以 `AdminOnly` policy 保護。自訂 Role 已可 CRUD 並指派帳號，但沒有 Permission model，因此無法賦予細粒度存取能力。既有 access session allowlist 是 authoritative registry；角色或其他安全狀態改變時，系統會撤銷受影響帳號的 access sessions 與 refresh families。

這項變更跨越 EF Core persistence、JWT issuance、ASP.NET Core authorization、管理 API、Vue auth state 與 Role Management UI，且必須避免錯誤設定造成管理功能永久鎖死。

## Goals / Non-Goals

**Goals:**

- 建立穩定、可稽核且由程式碼控制的 Permission catalog。
- 讓 `User → Roles → Permissions` 成為 API 授權的正式判斷鏈。
- 提供 Admin 可維護的 Role–Permission mappings 與清楚的前端操作面。
- 沿用既有 JWT/session 架構，確保 Permission 異動後舊 token 立即失效。
- 保護 system `Admin` role 永遠擁有完整 catalog，避免管理鎖死。

**Non-Goals:**

- 不提供 user-specific allow/deny、顯式 deny precedence 或 Role inheritance。
- 不處理 Todo ownership 等 resource-based authorization。
- 不提供可由 UI 任意建立、改名或刪除 Permission definition 的能力。
- 不處理 multi-tenancy、時間條件、審批流程或完整 authorization audit log。

## Decisions

### 1. Permission catalog 由程式碼定義，資料庫保存同步副本與 Role mappings

每個 Permission 使用穩定 kebab/dot key（例如 `users.read`、`users.manage`、`roles.manage`）及 display metadata。啟動初始化器以冪等方式 upsert catalog；已從程式碼移除的 key 不可繼續授權，資料清理由 migration 或明確同步策略處理。

相較於完全由資料庫／UI 動態建立 Permission，此做法可讓 endpoint policy、測試與部署版本保持一致，也避免拼字或刪除造成不可預期的安全行為。

### 2. 使用明確 Permission 與 RolePermission entities

新增 Permission table 與 Role–Permission join table，join table 對 `(RoleId, PermissionId)` 設唯一鍵並使用外鍵約束。Identity Role 維持角色來源；不以 Identity role claims table 當主要 mapping，因為獨立 model 更容易提供 catalog metadata、查詢與 migration 約束。

### 3. `Admin` 為隱含全權 system role

授權計算遇到 `Admin` role 時直接取得目前完整 catalog；API 不允許削減 Admin Permission。資料庫可同步 Admin mappings 供查詢展示，但安全判斷不依賴該 mappings 是否完整。`User` 與自訂 Roles 使用資料庫 mappings。

這比「允許修改 Admin 但保護最後一位完整管理者」更單純且不易鎖死；代價是 Admin 無法套用最小權限，需以自訂管理 Role 支援日常操作。

### 4. JWT 帶 Permission claims，並沿用 session 撤銷保證 freshness

登入、註冊與 refresh 時查詢使用者 Roles 的有效 Permissions，去重後寫入個別 permission claims，並回傳於 `AuthUser`。授權 request 不逐次查資料庫，而由 authorization handler 檢查 token claim；既有 allowlist 繼續驗證 `jti`、`sid` 與 user id。

Role–Permission mapping 成功異動後，服務在 transaction 邊界內找出所有受影響 users，更新 security stamps，並於 commit 後撤銷其 Memory sessions 與 persisted refresh families。如此保留目前 request path 的效能模型，代價是大型 Role 異動需批次處理撤銷。

### 5. 使用動態 Permission policy provider

endpoint 以穩定命名慣例要求 Permission，例如 `Permission:users.read`。自訂 policy provider 依 policy name 建立 requirement；handler 僅在 authenticated principal 具有對應 permission claim 時成功。未知 Permission key 必須 fail-closed，並在開發／測試階段由 catalog 驗證捕捉。

`AdminOnly` policy 暫時保留供 bootstrap 或明確需要 system Admin 的高風險操作；一般管理 endpoint 漸進改用相應 Permission policy。

### 6. Role Permission 設定採完整集合、冪等更新

管理 API 提交指定 Role 的完整 Permission key 集合。服務先驗證所有 keys 存在，再以 transaction 計算新增／移除差異；任一驗證或 persistence 失敗時保持原 mappings。對 `Admin` 提交非完整集合回傳 conflict；重複提交相同集合成功且不建立重複關聯。

### 7. 前端 Permission 僅用於 UX

前端 auth state 保存後端回傳的 Permission keys，集中提供 `hasPermission` 判斷導覽與操作顯示。這不取代 API policy；手動呼叫或修改 client state 仍必須由後端拒絕。

## Risks / Trade-offs

- [JWT claims 隨 Permission 數量增加而膨脹] → 第一階段維持精簡 catalog；若未來規模明顯增加，再評估 permission version 或 server-side distributed session data。
- [Role mapping 異動涉及大量帳號撤銷] → 使用集合查詢與批次處理，並在 transaction commit 後統一撤銷；加入負載與失敗測試。
- [程式 catalog 與資料庫不同步] → 啟動時冪等同步、唯一約束、未知 key fail-closed，並以 migration/integration tests 驗證。
- [既有 Admin endpoint 遷移時漏設 policy] → 建立 endpoint-to-permission 對照與 integration tests，逐項替換而非移除全域安全預設。
- [Admin 隱含全權不符合最小權限] → 將 Admin 定位為 break-glass/system administrator，日常管理建立自訂 Roles。

## Migration Plan

1. 新增 Permission 與 RolePermission schema migration，不改動現有 user/role relations。
2. 部署 catalog initializer，建立固定 Permission rows，並讓 `Admin` 立即取得完整有效 catalog。
3. 為既有管理行為定義 permissions；在切換 endpoint policy 前，將需要維持存取的自訂 Roles 明確配置 mappings。
4. 更新 token/auth payload 與前端 auth state，再逐一把管理 endpoints 改用 Permission policies。
5. 部署後既有 access token 不含 Permission claims，會在需要新 policy 時 fail-closed；可透過版本部署時撤銷既有 sessions，讓使用者以 refresh/login 取得新 claims。
6. Rollback 時先將 endpoints 恢復 `AdminOnly`，再回退應用程式；保留新增資料表不影響舊版，待確認無需資料後另行 migration 移除。

## Open Questions

- 第一批 Permission keys 的最終命名與 endpoint 對照需在實作前依現有 Users、Roles、Todos endpoints 盤點確認。
- 是否將 Permission display name／description 納入本地化，第一階段預設採固定英文 metadata。

