## Why

目前規格助理由畫面按鈕決定查規格或產生專案草稿，無法在單一對話中自行選擇工具、追問缺少欄位或延續後續 PMS 工作。導入 Semantic Kernel 與持久化對話基礎，可將現有 RAG 和安全草稿流程提升為全站可用、可逐步擴充的工作助手。

## What Changes

- 導入 Semantic Kernel 作為 ASP.NET Core 助手的 function-calling orchestration 層。
- 建立使用者綁定的持久化 conversation、message 與 pending action 狀態，支援跨頁面及重新開啟後延續對話。
- 將既有規格查詢與專案草稿流程包裝成語意清楚、權限受控的 Kernel plugins。
- 新增 Issue 草稿與可指派人員查詢 plugins，支援缺少欄位追問與多工具連續操作。
- 所有資料寫入維持預覽、一次性確認、後端權限與 service 驗證；Kernel 不直接操作資料庫。
- 在 Topbar 加入全域助手入口，桌面使用右側 Drawer、行動版使用全螢幕面板，並顯示可移除的目前頁面上下文。
- 保留並升級現有規格助理頁為完整 Assistant Workspace，與 Drawer 共用 conversation。

## Capabilities

### New Capabilities

- `assistant-conversation`: 使用者綁定的多輪對話、訊息、待辦動作及最近工作上下文生命週期。
- `assistant-tool-orchestration`: Semantic Kernel plugins、自動工具選擇、缺少欄位追問、連續工具呼叫與安全確認界線。
- `assistant-global-interface`: Topbar Drawer、行動版面板、頁面上下文及完整 Assistant Workspace 的共用體驗。

### Modified Capabilities

- 無。

## Impact

- Backend：新增 Semantic Kernel 套件與設定、conversation persistence、assistant endpoints、plugins、tool filters 與確認服務；沿用既有 RAG、Project、Issue、User services。
- Database：新增 conversation、message、pending action／confirmation 等資料結構與 EF Core migration。
- Frontend：AppTopbar／AppLayout 全域助手、PrimeVue Drawer、聊天狀態管理，以及既有規格助理頁升級。
- Security：工具依登入者權限動態暴露或拒絕執行；寫入工具只能產生預覽，確認端點才可呼叫既有業務 service。
- Tests：plugin contracts、工具選擇循環、對話隔離、上下文、確認冪等性、API、Drawer 與 Workspace 測試。
