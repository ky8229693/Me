# Project Memory

最後更新：2026-07-21

本文件只記錄無法輕易從程式碼看出、跨 OpenSpec changes 仍有價值的專案脈絡。開發規範以 [`AGENTS.md`](../AGENTS.md) 為準；指令與環境設定請見 [`development.md`](development.md)，前端設計原則請見 [`ui-guidelines.md`](ui-guidelines.md)。目前 change 與歷史紀錄應使用 OpenSpec CLI 與 `openspec/changes/archive/` 查詢，不在此手動維護清單。

## 核心架構決策

- API 暫時維持 Minimal APIs；只有當規模與路由複雜度產生實際需求時才評估 Controllers。
- API payload 使用共用 `ApiResponse<T>` envelope，同時保留正確 HTTP status code。
- `Program.cs` 聚焦 service registration、middleware、Swagger、CORS 與 endpoint wiring；feature routes、logic 與 cross-cutting concerns 分開放置。
- Swagger 端點以功能標籤分類；新增 API 時必須沿用對應 feature tag，管理功能可再細分 Users、Roles 等子領域。
- 後端功能持續增加時，Endpoints、Contracts/Models 與 Services 應改以 feature 為主要分類（例如 Auth、Administration、Todos），避免不同功能長期集中在共用資料夾；既有程式採漸進式搬移，不為分類而一次大改。
- PostgreSQL 是預設持久化資料庫；schema 透過 EF Core migrations 管理。
- 認證使用 ASP.NET Core Identity 與 JWT，不建立自行維護的密碼資料表。
- Local development 維持 HTTP only，不主動導入 HTTPS。
- Todo 目前仍由 `InMemoryTodoService` 保存；持久化與使用者 ownership 應由後續獨立 change 處理。

## 認證與管理安全決策

- API 啟動時確保固定的 `Admin`、`User` roles 存在；新帳號預設取得 `User`。
- JWT access token 預設存活 15 分鐘，包含 Identity roles、唯一 `jti` 與 refresh family `sid`；標準 JWT 驗證成功後，authenticated request 只接受存在於單機 Memory session allowlist 且 `jti`、`sid`、user id 全部相符的 session，不再於每次 request 查詢 Identity user 或 PostgreSQL security stamp。
- Refresh token 使用 7 天固定 absolute lifetime 與每次使用即 rotation；原文只存在 `HttpOnly`、`SameSite=Strict`、`Path=/api/auth` cookie，PostgreSQL 只保存 SHA-256 hash。Production cookie 強制 `Secure`，Development 因 HTTP only 允許非 Secure。
- Refresh token family 的已消耗 token 若再次出現，視為 reuse，撤銷整個 family 及其 Memory access sessions；refresh 時才重查 Identity user 狀態與目前 roles。
- 帳號停用、重新啟用、角色指派、Role rename、Admin bootstrap promotion 或其他 security mutation 成功後，必須撤銷受影響 user 的全部 Memory sessions，使舊 token 在下一次受保護 request 立即失效。
- 人工停用目前沿用 Identity lockout 欄位；若未來加入登入失敗自動鎖定，需評估改用獨立的 active status。
- 第一位 Admin 只能透過非 HTTP 的 `bootstrap-admin` command 建立或提升；系統不提供預設 Admin credentials 或匿名 bootstrap API。
- 管理操作禁止 Admin 停用自己、移除自己的 Admin role，並保護最後一位啟用中的 Admin。
- `Admin`、`User` 是不可改名或刪除的 system roles；Admin 可維護其他自訂 roles。使用中的 role 必須先移除全部帳號指派才能刪除。
- 自訂 role 只建立 Identity role 名稱與帳號關聯，不會自動授予 Permission、建立 policy 或產生 API 存取權限。
- Role rename 會更新所有受影響帳號的 security stamp，使其舊 JWT 在下一次受保護 request 失效。
- 前端依 roles 控制導覽可見性只屬 UX；所有管理 API 仍以 `AdminOnly` policy 作為安全邊界。
- Memory session allowlist 是 authoritative registry，不是可在 miss 時回查資料庫並重新啟用 token 的一般 cache；unknown、expired 或 evicted session 一律 fail-closed。
- 目前 allowlist 只支援單一 API process；API restart、deployment 或 Memory eviction 會讓既有 access token fail-closed，但瀏覽器仍可用有效 refresh cookie 建立新的 Memory session。跨 instance 需另行導入 distributed registry。

## 前端與 UI 原則

- 前端使用 Vue 3 Composition API、TypeScript、PrimeVue styled mode 與 Aura theme。
- 後台 layout 參考 Sakai Vue 的 topbar、sidebar、menu、table 與 CRUD 組合，但不直接複製完整 template。
- Layout 元件保持拆分；desktop sidebar collapse 與 mobile overlay drawer 應使用不同語意的狀態。
- 導覽由資料模型產生並由 layout 集中管理 active state 與授權可見性。
- 常見操作優先使用現有 PrimeVue components，視覺沿用 Aura theme 與既有 CSS variables，避免沒有需求的額外 UI dependencies。
- 新增 UI 互動時補 unit tests；desktop 與 mobile 行為不同時，至少固定 state class、navigation event 與授權顯示條件。
- Vue Router history mode 是前端導覽狀態的唯一來源；Project、Issue 與管理頁面必須具備可直接開啟及重新整理恢復的 URL。
- 全站側欄依工作台、專案管理、系統管理分組；Project 內使用 Overview、Issues、Members、Settings contextual navigation。
- Project 與 Issue 詳情是完整頁面，不使用 Dialog；Dialog 只用於短建立、確認或選擇，且不得形成 nested modal。
- Issue list 的搜尋、篩選與分頁保存在 route query，以支援返回清單及分享目前視圖。

## PMS Issue Foundation

- Issue 直接隸屬 Project；Project membership 是唯一資料存取邊界，System Admin 沒有跨 Project bypass。
- Issue type 固定為 Feature、Bug、ChangeRequest；priority 固定為 Low、Medium、High、Critical。
- 第一版 workflow 固定為 Draft → Approved → InProgress → QA → Done，僅支援規格明列的退回與重新開啟，不提供自訂 workflow。
- Owner／Manager 管理所有 Issue；Member 可建立、編輯自己的 Draft、留言及推進指派給自己的工作；Viewer 唯讀。
- Issue assignee 必須是目前 Project member；移除成員時清除該 Project 的 Issue assignments 並保留 append-only history。
- Project 封存後 Issue、assignment、workflow 與 comments 全部唯讀；Issue 與 comment 不提供刪除，comment 亦不提供修改。
- Issue foundation 不包含附件、通知、子任務、標籤、工時、看板、文件／API 關聯、報表或 AI；這些需以後續 change 規劃。

## 當前技術限制

- Todo 尚未持久化，也尚未與登入使用者建立 ownership。
- 授權已有自訂 role CRUD，但尚無 Permission model、role-permission mapping、user-specific permissions 或 resource-based authorization。
- Access token 只保存在前端 module memory，不寫入 localStorage；啟動時會清除舊 `web_demo.auth_token` key，並以 refresh cookie 恢復登入。
- Shared HTTP client 對可重試的 401 使用單一 JavaScript runtime 內的 single-flight refresh，成功後原 request 最多重試一次；多分頁／多 tab 協調與跨 instance distributed session registry 尚未實作。
- 人工停用與未來可能加入的登入失敗 lockout 尚未拆成不同狀態。
- E2E 使用現有 PostgreSQL server，但每個完整 Playwright run 建立唯一的 `web_demo_e2e_*` database；測試服務使用獨立 ports，結束後刪除整個 database。
- Frontend 尚未導入 Pinia、request cancellation、retry 或 cache；只有規模產生需求時才評估。

## 後續方向

- Todo PostgreSQL persistence、migration 與 ownership。
- 正式 structured logging。
- Permission model 與 resource-based authorization。
- 登入失敗 lockout 與人工停用狀態分離。

這些方向不是承諾中的 active scope；開始實作前仍需建立或確認對應 OpenSpec change。
