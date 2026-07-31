## Context

`authorization-foundation` 已建立 `Admin`、`User` roles、`AdminOnly` policy、role-aware JWT 與最小 Admin status endpoint，但 production 沒有第一位 Admin 的安全建立方式，也沒有帳號或角色管理操作。現有 Identity schema 已提供 users、roles、user-role relations、lockout 與 security stamp；前端為 Vue 3 + TypeScript + PrimeVue，API 維持 Minimal APIs 與 shared `ApiResponse<T>`。

此 change 橫跨命令列啟動模式、Identity 管理、JWT validation、Admin APIs、前端後台與測試。帳號停用及角色收回必須同步處理既有 JWT，否則在 token 到期前管理操作不會真正生效。

## Goals / Non-Goals

**Goals:**

- 安全建立或提升第一位 Admin，且不提供預設 credentials 或匿名 HTTP bootstrap。
- 提供 Admin-only 帳號搜尋、分頁、明細、啟用與停用。
- 提供固定 `Admin`／`User` role 指派及最後 Admin／自我操作保護。
- 讓停用帳號或角色異動立即使既有 JWT 失效。
- 提供符合現有 PrimeVue 後台風格的最小帳號管理 UI。
- 以後端 integration、前端 unit 與少量 E2E tests 固定安全行為。

**Non-Goals:**

- 不提供自訂 role CRUD、Permission table、User-specific permissions 或多租戶授權。
- 不提供硬刪除帳號、Email 寄送、忘記密碼或管理員直接指定新密碼。
- 不建立預設 Admin 帳密，也不將 bootstrap 密碼保存於設定檔或 logs。
- 不導入 controller、Pinia 或新的 UI framework。

## Decisions

### 將三項能力放在同一 change，但依相依性分階段

實作順序固定為 token invalidation 基礎、Admin bootstrap、管理 API、前端操作面。Bootstrap 讓系統先有合法操作者；帳號狀態與 role assignment 共用相同管理 service、安全規則與 UI，因此以一個 change 交付但在 tasks 中維持清楚階段。

替代方案是三個獨立 changes；邊界較小，但帳號管理與角色指派會重複建立 contracts、查詢、頁面與 token 失效機制。

### Bootstrap 使用 application command mode，不提供 HTTP endpoint

API entry point 辨識明確的 bootstrap arguments，在一般 web host 接受 requests 前建立 scope 並執行 `AdminBootstrapService`。新帳號的密碼由互動式隱藏輸入取得；自動部署可使用一次性 process environment input，但不得寫入 tracked settings 或輸出內容。若指定 Email 已存在且系統尚無 Admin，command 需明確確認後只提升該帳號。

只要已存在 Admin，bootstrap 即拒絕任何變更。此限制將 bootstrap 定位為第一位管理者救援入口，日常角色調整必須走 authenticated Admin API。

替代方案是匿名一次性 endpoint；即使以設定 flag 保護，仍增加遠端攻擊面與部署後忘記關閉的風險。

### 使用 Identity lockout 欄位表示管理停用狀態

停用帳號使用 `UserManager.SetLockoutEndDateAsync` 設定遠期 lockout，啟用則清除 lockout end，沿用 `SignInManager` 的既有登入檢查與 Identity schema。管理 response 將其轉換為明確的 `IsEnabled`，不向前端暴露 Identity 內部欄位。

替代方案是擴充自訂 `ApplicationUser.IsActive`；語意較直接，但需要更換泛型 user type 與 migration，本批既有 Identity lockout 已足以支援人工停用。

### JWT 加入 security stamp 並在 protected request 驗證

Token service 加入集中管理的 security stamp claim。JWT `OnTokenValidated` 依 user id 載入 Identity user，確認帳號存在、未停用且 security stamp 相符；失敗時 authentication context fail。停用、啟用或角色異動成功後呼叫 `UpdateSecurityStampAsync`，使先前 token 下一次呼叫即失效。

此選擇會讓每個 bearer-authenticated request 多一次 user lookup，但提供立即撤銷語意且不需新增 token blacklist table。未來流量成長時可改採短效 access token、refresh token 與 token-version cache。

替代方案是只等待目前 60 分鐘 JWT 到期；對 Admin 降級或帳號停用而言風險不可接受。

### Admin user service 集中查詢與安全不變量

新增 scoped service 封裝 `UserManager`、`RoleManager` 與 `AppDbContext` 操作。Endpoints 只處理 request validation、目前 user id、HTTP status 與 envelope mapping。帳號清單先分頁查 users，再取得該頁 roles；第一版接受每頁額外 role queries，預設及最大 page size 必須有限制。

停用 Admin 或移除 Admin role 前，在資料庫 transaction 中確認啟用中 Admin 數量。禁止目前操作者停用自己或移除自己的 Admin role；所有帳號必須保留 `User` role，未知 role 在任何 mutation 前即拒絕。

替代方案是在 endpoint 直接呼叫 `UserManager`；會讓最後 Admin、自我保護與 stamp 更新規則散落且難以單元測試。

### 角色設定採完整集合 PUT

使用 `PUT /api/admin/users/{id}/roles` 提交目標帳號期望的完整 roles 集合。Service 比對現況後只新增缺少項、移除多餘項，使 request 冪等；只允許 `Admin` 與 `User` 且強制包含 `User`。

替代方案是 add/remove endpoints；操作較直觀，但前端需要多次 request，部分成功時較難保持一致。

### 前端由後端提供 current roles，但安全邊界仍在 API

擴充 `AuthUser` 讓 register、login 與 `/api/auth/me` 回傳 roles。前端用 roles 顯示 Admin 導覽及操作，但所有 `/api/admin/users` endpoints 仍要求 `AdminOnly` policy；隱藏 UI 不視為授權控制。

帳號管理 API 集中於 frontend API module，頁面使用 PrimeVue DataTable、Paginator、InputText、Tag、Dialog／ConfirmDialog 與 Toast 等既有元件模式，不解析 Identity 內部欄位。

## Risks / Trade-offs

- [每個 authenticated request 增加 Identity user lookup] → 第一版優先確保撤銷正確性；以 index user id 查詢並在效能有實際壓力時再引入 cache／refresh token。
- [Lockout 同時承擔登入失敗鎖定與人工停用] → 本批尚未啟用失敗鎖定；對外只暴露 `IsEnabled`，未來導入自動 lockout 時再拆分狀態模型。
- [同時操作最後兩位 Admin 可能產生競態] → 以 transaction 包住有效 Admin 計數與 mutation，並加入 service-level concurrency 測試或 PostgreSQL 驗證情境。
- [Bootstrap process environment 可能被主機工具讀取] → 優先使用互動式隱藏輸入，automation input 僅一次性存在且不得記錄；production secret manager 留待部署平台確定後整合。
- [E2E 會在 local PostgreSQL 留下帳號] → 使用明確唯一前綴；管理 E2E 只建立必要帳號，專用 E2E database 與清理 fixture 留待測試資料治理 change。
- [角色或狀態更新部分失敗] → mutation service 在 transaction 中執行，只有全部成功才回傳成功，失敗時保留原狀並記錄非敏感錯誤。

## Migration Plan

1. 擴充 auth contracts、security stamp claim 與 token validation，加入舊 token／停用帳號測試。
2. 新增 bootstrap command 與 service，先在 local database 建立第一位 Admin 並驗證重跑拒絕。
3. 新增帳號查詢、狀態與 role assignment service/endpoints，以及最後 Admin 安全測試。
4. 新增前端 Admin API、帳號管理頁與 roles-aware navigation。
5. 執行 migrations 狀態確認、backend/frontend tests、Playwright、build 與 OpenSpec validation。

回復時先停止使用管理 UI/endpoints，再移除 bootstrap command 與 stamp validation。既有 Identity users、roles、lockout 狀態不需 schema rollback；若有人工停用帳號，回復前需明確決定是否解除 lockout。

## Open Questions

- 部署平台確定後，需決定 automation bootstrap password 採哪一種 secret provider；本地第一版以隱藏互動輸入為主。
- 未來加入登入失敗鎖定時，需評估將人工停用狀態移至自訂 `ApplicationUser.IsActive`。
- 專用 PostgreSQL E2E database 與測試帳號清理策略應由後續 test-data-governance change 處理。
