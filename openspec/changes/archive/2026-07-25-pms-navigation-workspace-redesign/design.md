## Context

目前 `App.vue` 以 `activeView` 在記憶體切換 Dashboard、Projects、Users 與 Roles；`AppMenu` 只能渲染單一 `Home` 區段。`ProjectsView` 同時負責 Project 清單、Project 編輯、members 與 Issue workspace，並以 Project Dialog 包住 Issue 的 create/detail Dialog。這個結構在功能少時可運作，但已無法提供穩定 URL、瀏覽器返回、重新整理恢復與可擴充的 Project context。

本次維持 Vue 3、TypeScript、Vite、PrimeVue Aura 與既有 HTTP/API contracts。視覺 shell 延續 Sakai Vue；資訊架構參考成熟 PMS 的「全站導覽 → Project workspace → resource page」層級，不複製其完整功能或樣式。

## Goals / Non-Goals

**Goals:**

- 以 Vue Router 建立可直接開啟、分享、重新整理與返回的頁面導覽。
- 依工作台、專案管理、系統管理分組全站功能。
- 將 Project 與 Issue 詳情提升為完整 route-level page。
- 在 Project 內建立可擴充但不預先暴露未實作功能的 contextual navigation。
- 將 Dialog 限制為短操作，消除 nested modal。
- 沿用現有後端 authorization，並以 route meta／API response 改善前端 UX。

**Non-Goals:**

- 不重新設計 Project、membership 或 Issue API、資料模型與權限規則。
- 不加入 board、gantt、documents、API specs、reports 或 AI 頁面。
- 不實作使用者自訂 sidebar、收藏、最近瀏覽或 command palette。
- 不一次重寫所有 PrimeVue styling 或導入另一套 UI framework。
- 不讓前端 route guard 取代 API authorization。

## Decisions

### 1. Vue Router 成為導覽狀態的唯一來源

新增 `vue-router`，使用 history mode，建立具名稱的 routes。`App.vue` 只處理 session bootstrap 與 layout，不再維護 `activeView`。Route components 透過 params 取得 `projectId`、`issueId`，篩選、頁碼與返回位置透過 query string 保存。

替代方案是延伸既有 `activeView` 與 selected object，但無法可靠處理 deep link、browser back 或 refresh，因此不採用。

### 2. 全站導覽使用 group model

menu model 改為 `MenuGroup[]`，每組包含 label 與 route items：

- 工作台：Dashboard。
- 專案管理：Projects。
- 系統管理：Users、Roles、Permissions；每項依既有 permission 顯示。

若某 group 沒有任何可見 item，整組不渲染。Active state 從目前 route 計算，不在 menu item 保存 mutable boolean。

### 3. Project workspace 使用 nested routes 與固定 context header

路由結構：

- `/projects`
- `/projects/:projectId`（redirect 至 overview）
- `/projects/:projectId/overview`
- `/projects/:projectId/issues`
- `/projects/:projectId/issues/:issueId`
- `/projects/:projectId/members`
- `/projects/:projectId/settings`

`ProjectWorkspaceView` 載入 project context，呈現 breadcrumb、名稱、status、current role 與子導覽，再以 nested `RouterView` 顯示內容。Overview、Issues、Members 對所有 Project members可見；Settings 僅 Owner／Manager出現在導覽，Owner-only 操作仍由元件與 API 個別限制。

未加入 route 的未來功能不顯示 disabled placeholder，避免導覽充滿尚不可用項目。

### 4. Issue list 與 Issue detail 是不同頁面

Issue list 保留 DataTable 與 server-side filters；filter、page、pageSize 寫入 route query。點擊 Issue 導航到 full-page detail，detail 顯示內容、workflow、comments 與 history，並提供 breadcrumb／Back to Issues。

返回清單時沿用原 query，保留使用者的工作位置。第一版不做 Linear-style Peek drawer；若日後需要快速預覽，再以獨立 change 加入，避免同時保留 modal 與 full page 造成兩套 detail state。

### 5. Dialog 使用明確邊界

Dialog 只允許：快速建立 Project／Issue、危險操作確認、短選擇流程。Project detail、Issue detail、member administration 與 project settings 必須是 page content。任何 Dialog 內不得再觸發另一個 Dialog；需要長流程時導航到完整頁面。

### 6. Session 與 route authorization 分層

應用啟動完成 refresh session 後才解析受保護頁面。未登入導向 login；欠缺 system permission 的管理 route 導向可存取首頁或顯示 Forbidden。Project route 不以 JWT system role 推測 membership，而是載入 Project API；404／403 呈現明確頁面狀態。

所有安全判斷仍由 API 執行，route guard 僅避免無效導覽與改善體驗。

### 7. 漸進式拆分而非一次重寫樣式

先拆出 router、route views、Project workspace shell、Issues list/detail、Members 與 Settings。既有 API client 與 PrimeVue components 繼續使用；CSS 只調整 page layout、context navigation 與 responsive behavior。完成後再刪除舊 `activeView`、Project detail Dialog 與 Issue detail Dialog。

## Risks / Trade-offs

- [導入 Router 會影響既有 App 與 component tests] → 先建立 route map 與 test router helper，再逐頁遷移並更新測試。
- [Vite history mode 直接重新整理可能由伺服器回傳 404] → Development 由 Vite fallback；部署文件必須記錄 production host 將非 `/api` routes fallback 到 `index.html`。
- [Project context 在多個子頁重複取得] → 由 workspace shell 載入並透過 provide/inject 或 route-scoped composable 共用；不為此提前導入 Pinia。
- [URL query 與 UI filter 可能互相循環更新] → 定義單向初始化與 replace/push 規則，並加 router tests。
- [完整頁面比 modal 多一次導航] → 以保留 query、breadcrumb 與 browser back 換取穩定定位；未來可另加 peek drawer。
- [同時存在兩個 active OpenSpec changes] → 本 change 不修改 Issue domain behavior；完成 Issue Foundation 人工驗證後可獨立封存，不互相混合 tasks。

## Migration Plan

1. 安裝 Vue Router，建立 routes、route meta 與 auth bootstrap integration。
2. 將全站 menu 改為 group model，先讓 Dashboard／Projects／admin pages 可透過 URL 開啟。
3. 建立 Project workspace shell 與 Overview／Members／Settings pages。
4. 拆分 Issues list 與 Issue detail routes，將 filter/page 狀態移至 query。
5. 移除舊 `activeView`、Project detail Dialog 與 Issue detail Dialog。
6. 更新 unit tests、build、deep-link 與 responsive manual verification。

若需回退，保留 API client 與 domain components，還原至上一版本的 `App.vue` view switching；本 change 無 DB migration 或後端 rollback。

## Open Questions

- 是否在後續 change 加入 Linear-style right-side Peek；第一版先只做 full-page detail。
- Permissions 頁面目前若尚無獨立 route/view，系統管理群組先只顯示已實作的 Users、Roles，待 Permission UI 存在後加入。
- Production 最終由何種 web host 提供 SPA fallback，部署時需依實際 hosting 補設定。
