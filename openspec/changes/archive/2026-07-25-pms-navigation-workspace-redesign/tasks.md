## 1. Router 基礎

- [x] 1.1 安裝與註冊 Vue Router，使用 history mode
- [x] 1.2 定義 Dashboard、Projects、Users、Roles、Project workspace、Issue detail 與 fallback named routes
- [x] 1.3 建立 route meta 型別與 system permission requirements
- [x] 1.4 將 session bootstrap 與登入／登出流程整合到 router，不在 session 尚未恢復時渲染受保護頁面
- [x] 1.5 建立 Not Found、Forbidden 與 routed loading/error 共用頁面狀態

## 2. 全站分組導覽

- [x] 2.1 將 MenuItem model 改為 route-based item，新增 MenuGroup model
- [x] 2.2 將 AppMenu 改為渲染工作台、專案管理、系統管理群組並省略空群組
- [x] 2.3 依 users.read、roles.read 與現有 permissions 控制各管理項目可見性
- [x] 2.4 從目前 route 計算 active menu state，移除 menu item 的 mutable active 欄位
- [x] 2.5 將 AppLayout navigation events 改為 RouterLink／router navigation 並保留 mobile sidebar close 行為
- [x] 2.6 重構 App.vue 使用 RouterView，移除 activeView view switching

## 3. Project Workspace

- [x] 3.1 將 ProjectsView 縮減為 Project 清單與短建立 Dialog，選取 Project 時導航到 workspace
- [x] 3.2 建立 ProjectWorkspaceView，載入 Project context 並提供 child routes 共用資料
- [x] 3.3 建立 Project breadcrumb、context header、status、current role 與 responsive workspace navigation
- [x] 3.4 建立 Project Overview 頁面，呈現基本資料與可用工作入口
- [x] 3.5 將既有 Members 管理抽出為 Project Members 完整頁面
- [x] 3.6 將 Project 編輯、archive 與 restore 抽出為 Project Settings 完整頁面
- [x] 3.7 只對 Owner／Manager顯示 Settings，並在 Settings 內維持 Owner-only archive／restore 行為
- [x] 3.8 在所有 Project 子頁一致呈現 Archived read-only 狀態並隱藏不可用 mutations
- [x] 3.9 處理 Project loading、Not Found、Forbidden 與一般 API 錯誤，不殘留前一個 Project context

## 4. Issue List 與 URL 狀態

- [x] 4.1 將 IssueWorkspace 拆分為 route-level IssueListView、IssueDetailView 與可重用的 create form
- [x] 4.2 在 Project Issues route 建立 DataTable、server-side pagination 與既有 filters
- [x] 4.3 將 search、type、status、priority、assignee、page 與 pageSize 同步至 route query
- [x] 4.4 從 route query 初始化 Issue list，避免 UI/router 更新循環並保留瀏覽器歷程行為
- [x] 4.5 點擊 Issue 時導航至 project-scoped Issue detail route並攜帶可恢復的 list query
- [x] 4.6 保留單層快速建立 Issue Dialog，建立成功後更新 list 或導航至新 Issue

## 5. Issue Full-page Detail

- [x] 5.1 建立 IssueDetailView，直接載入 route params 指定的 Issue、history 與 comments
- [x] 5.2 在完整頁面呈現 Issue 編輯、assignee、due date 與 API 回傳的 allowed transitions
- [x] 5.3 在完整頁面呈現 comments 與 append-only history，不使用額外 Dialog
- [x] 5.4 建立 Issue breadcrumb 與 Back to Issues，返回時還原原 list query
- [x] 5.5 處理 Issue loading、Not Found、Forbidden、Archived readonly 與一般 API 錯誤
- [x] 5.6 移除 Project detail Dialog、Issue detail Dialog 及所有 nested modal 路徑

## 6. Responsive Layout 與可用性

- [x] 6.1 調整 workspace header、sub-navigation、tables 與 detail columns 的 desktop layout
- [x] 6.2 驗證 mobile sidebar、workspace navigation、filter controls 與 detail content 不造成水平溢出
- [x] 6.3 確保頁面標題、breadcrumb、active state、buttons 與 form controls 具有可辨識文字或 aria label
- [x] 6.4 確認 Dialog 僅用於建立、確認或短選擇，且任何 Dialog 不再觸發第二層 Dialog

## 7. 自動化測試

- [x] 7.1 建立 memory-history router test helper 與 authenticated route mounting utilities
- [x] 7.2 新增 sidebar 分組、empty group、permission visibility 與 active route tests
- [x] 7.3 新增 session restore、login redirect、logout 與 unauthorized administration route tests
- [x] 7.4 新增 Project list navigation、workspace child routes、membership Not Found 與 Archived state tests
- [x] 7.5 新增 Issue filter/query synchronization、pagination與返回清單狀態 tests
- [x] 7.6 新增 Issue deep-link、detail actions、comments/history及 Not Found tests
- [x] 7.7 新增 modal boundary test，確認 Project／Issue detail 不以 Dialog 渲染且無 nested modal

## 8. 驗證與文件

- [x] 8.1 執行 frontend unit tests 並修正本次重構造成的 regression
- [x] 8.2 執行 npm run build，確認 TypeScript 與 production bundle 成功
- [x] 8.3 執行 dotnet test WEB_Demo.sln，確認前端重構未影響 API contracts
- [ ] 8.4 手動驗證 deep link、refresh、Back／Forward、desktop 與 mobile navigation
- [ ] 8.5 以 Admin、一般使用者、Owner、Manager、Member、Viewer 及非 Project member 驗證導覽可見性
- [x] 8.6 更新 development／deployment 文件，記錄 frontend routes 與 production SPA fallback requirement
- [x] 8.7 更新 docs/memory.md，記錄 route-based navigation、Project workspace 與 Dialog boundary 決策
- [x] 8.8 執行 openspec validate 並核對所有 acceptance scenarios 有對應實作或測試
