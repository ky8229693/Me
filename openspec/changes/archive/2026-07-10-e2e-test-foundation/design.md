## Context

專案目前以 Vitest 測試 Vue 元件與 API helper，並以 xUnit 測試 ASP.NET Core service 及 endpoints，但尚未從真實瀏覽器驗證前後端整合。E2E 測試必須沿用 HTTP-only local URLs、Vite `/api` proxy、PostgreSQL-backed Identity，以及目前仍為 process-local 的 `InMemoryTodoService`。

## Goals / Non-Goals

**Goals:**

- 建立可由單一 npm script 執行的 Playwright E2E 基礎。
- 自動啟動或重用 API 與 Vue server，並等待服務可用後才開始測試。
- 覆蓋註冊、登出後重新登入，以及 Todo 新增、完成與刪除的主要流程。
- 讓測試帳號與測試資料具唯一性，降低重跑及平行執行造成的衝突。
- 保留失敗時可用的 trace、screenshot 等除錯資訊。

**Non-Goals:**

- 本批不建立完整 CI pipeline 或容器化 PostgreSQL。
- 不涵蓋 Role、Permission、密碼重設或使用者 ownership。
- 不改變現有 UI、API contracts、認證行為或 Todo 儲存方式。
- 不追求所有畫面與瀏覽器的完整測試矩陣；第一批以 Chromium 主要流程為準。

## Decisions

### Playwright 安裝於 Vue client

將 `@playwright/test` 加入 `src/WEB_Demo.Client` 的 devDependencies，並把設定與 E2E tests 放在 client 目錄內。瀏覽器行為與前端 package scripts 關係最直接，且可避免新增另一個 Node workspace。

替代方案是建立 repo-root E2E package；目前規模尚不需要額外 workspace 與重複 lockfile 管理。

### 使用 Playwright webServer 管理前後端程序

設定兩個 `webServer`：ASP.NET Core API 使用 `http://localhost:5186`，Vite client 使用 `http://127.0.0.1:5173`。本機允許重用已存在的 server，CI 則要求由測試命令啟動乾淨程序。

替代方案是要求開發者手動啟動兩個服務；這會讓測試容易因漏啟服務或啟動順序而失敗，無法達成單一指令執行。

### 以唯一 Email 隔離認證測試資料

每個測試流程以時間戳與隨機片段產生唯一 Email，透過可見 UI 註冊，再驗證登出與登入。第一批不直接清除 Identity table，避免 E2E 測試具備資料庫管理權限或依賴內部 schema。

代價是 local database 會留下少量測試帳號；未來建立專用測試資料庫或 database fixture 時再加入清理策略。

### 第一批採序列式 Chromium 主要流程

由於 Todo 仍存放於 singleton `InMemoryTodoService`，且目前沒有 user ownership，第一批測試以單一 Chromium project 與可辨識的唯一 Todo title 執行。測試只操作自己建立的 Todo，避免碰觸既有資料。

替代方案是立即建立跨瀏覽器和平行測試矩陣；在共享 in-memory state 下會增加不必要的不穩定性，應延至 Todo 持久化與隔離策略完成後。

### 優先使用可存取性 selector

測試以 role、label、placeholder 與可見文字定位元件；只有缺乏穩定語意時才加入 `data-testid`。這能同時驗證 UI 的基本可存取性並降低對 CSS 結構的耦合。

## Risks / Trade-offs

- [PostgreSQL 未啟動會使認證流程失敗] → 文件明確列出前置條件，Playwright server health check 只負責 API/Vite，不隱藏資料庫錯誤。
- [唯一 Email 會累積測試帳號] → 使用明確的 E2E Email 前綴，未來可安全識別並由專用清理流程移除。
- [重用本機 server 可能沿用舊狀態] → 測試資料使用唯一名稱；CI 禁止重用既有 server。
- [PrimeVue DOM 變動可能影響 selector] → 優先使用使用者可見語意，必要時才加入最少量的 test id。
- [前端 bundle 或 API 首次啟動較慢] → 設定合理 webServer timeout，測試本身不使用任意固定 sleep。

## Migration Plan

1. 安裝 Playwright dev dependency 並加入設定與 scripts。
2. 安裝 Chromium browser binary。
3. 建立 auth 與 Todo E2E tests。
4. 在 local PostgreSQL 可用時執行完整 E2E 測試。
5. 更新 README 或 memory 文件的執行方式。

若需要回復，只需移除 Playwright dependency、設定、E2E tests 與 scripts；不影響既有應用程式 runtime。

## Open Questions

- CI 平台確定後，再決定 PostgreSQL service、browser cache 與 artifact 上傳方式。
- Todo 改為 PostgreSQL persistence 後，再評估是否擴大平行執行與加入跨瀏覽器矩陣。
