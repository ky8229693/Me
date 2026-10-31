## Context

目前助手有兩條由 UI 按鈕選擇的流程：RAG 規格問答與專案建立草稿。專案草稿已具備預覽、使用者綁定、到期與一次性確認，但沒有共用 conversation、工具自動選擇、缺少欄位追問、Issue 草稿或全域入口。目標是建立 ASP.NET Core 內的受控 Semantic Kernel orchestration，而非讓自主 Agent 直接修改 PMS 資料。

## Goals / Non-Goals

**Goals:**

- 單一聊天訊息端點可依內容選擇規格、專案、Issue 與人員查詢工具。
- 支援缺少必要欄位的多輪追問，並在 Drawer 與 Workspace 間延續同一 conversation。
- 持久化對話與 pending action，讓重新整理、切頁或 API 重啟後仍可恢復。
- 將目前 route/project/issue context 以明確、可移除且經授權驗證的資料提供給助手。
- 所有寫入先建立結構化預覽；確認後才由既有 application service 執行。
- 對工具呼叫設置授權、迭代上限、日誌與安全輸出界線。

**Non-Goals:**

- 完全自主執行資料異動、刪除或批次修改。
- 多 Agent 協作、背景長時間任務或任意程式碼／SQL／HTTP 執行。
- 以向量資料庫保存聊天記憶；規格 RAG 繼續使用既有 pgvector。
- 第一批支援專案或 Issue 的修改、刪除、封存與狀態轉換。

## Decisions

### 1. Semantic Kernel 僅負責 orchestration

使用 native plugins 封裝既有服務，Kernel 負責把工具 schema 提供給模型、處理 function-calling loop 並回傳自然語言。Plugin 不直接使用 `AppDbContext` 寫入；業務驗證、權限、交易和 audit 繼續由既有 services 負責。相較自製 intent router，這可支援後續增加工具與連續呼叫；相較完全自主 Agent，行為邊界更明確。

### 2. 第一批工具分成讀取與準備兩類

- `SpecificationPlugin.search_specifications`
- `ProjectPlugin.search_projects`
- `ProjectPlugin.prepare_project_creation`
- `IssuePlugin.prepare_issue_creation`
- `UserPlugin.search_assignable_users`

讀取工具可自動執行。`prepare_*` 只能建立 pending action 和預覽，不得寫入 PMS。真正的 `confirm` 不提供為模型可自由選擇的 Kernel function，而由明確確認 API 執行。

### 3. 缺少欄位由工具合約判定

Plugin／domain validator 回傳結構化 `missingFields`、歧義候選或驗證錯誤，模型只負責轉成對使用者友善的追問。不可只靠 prompt 記得必要欄位，也不可讓模型虛構 projectId、userId 或其他識別碼。

### 4. 對話與業務狀態使用 PostgreSQL 持久化

新增 conversation、message 與 pending action entities。所有資料綁定 user id；API 以找不到回應隔離其他使用者。pending action 保存型別、經驗證的 payload、預覽、到期時間、狀態與 idempotency key。聊天歷史傳給模型時採固定上限，完整紀錄仍保留於資料庫供 UI 載入。

### 5. 一次只允許一個待確認寫入

每個 conversation 同時最多有一個 active pending action。新寫入要求若與既有草稿衝突，助手必須要求使用者先確認或取消，避免跨任務欄位混用。確認採原子狀態轉換，重送不得重複建立資料。

### 6. 頁面上下文是可信提示，不是授權依據

Frontend 傳送 route、projectId、issueId 等 context；Backend 重新確認登入者可存取後，才保存成 conversation context 並傳給 Kernel。Drawer 明確顯示目前範圍並允許移除。切換至另一專案時，不自動改寫尚未確認 action 的目標。

### 7. Topbar Drawer 與 Workspace 共用 store 和 API

`AppLayout` 持有全域 assistant shell；桌面使用右側 PrimeVue Drawer，窄螢幕使用全寬。既有 Spec Assistant route 演進為 Assistant Workspace，提供完整歷史、來源、索引狀態與較寬的預覽。兩者以 conversation id 讀寫同一資料，不建立各自的聊天狀態。

### 8. Kernel 執行必須有安全護欄

每次訊息只暴露目前使用者可用的工具；限制 function-call round trips 與工具結果大小；記錄 function 名稱、耗時、成功狀態和安全錯誤碼，不記錄 credentials、embedding 或不必要的文件全文。模型文字不得被視為成功證據，UI 只根據結構化 tool／confirmation 結果顯示已完成。

## Risks / Trade-offs

- [模型選錯或重複選工具] → 使用清楚且不重疊的 function descriptions、限制迭代次數、加入 tool-call tests，寫入只產生草稿。
- [聊天歷史持續增長導致成本與延遲] → 模型上下文採固定最近訊息窗口，後續需要時再加入摘要策略。
- [頁面 context 指向無權限或已不存在的資料] → 每次使用前由後端重新授權與解析，不信任 client 名稱或角色。
- [多分頁同時確認造成重複寫入] → pending action 使用原子狀態與 idempotency key，只有第一個確認能執行。
- [Kernel 或 provider 版本變動] → 將 Kernel 封裝在 assistant orchestration service，plugins 依賴既有 application services，避免 endpoint 與 UI 綁定 SDK 型別。
- [Drawer 空間不足] → 短流程留在 Drawer，提供以相同 conversation 在完整 Workspace 開啟的操作。

## Migration Plan

1. 新增資料表與 Kernel infrastructure，保留既有 query／project-draft endpoints 可暫時回退。
2. 將既有 RAG 與專案草稿能力包成 plugins，加入新 conversation API。
3. 加入 Issue 草稿與人員查詢、確認服務，再啟用 Topbar Drawer 與 Workspace 共用對話。
4. 驗證新流程後，才移除 UI 的舊雙按鈕模式；舊 API 是否保留由相容性測試決定。
5. 回滾時關閉新助手入口並回到既有 Spec Assistant route；migration 不刪除既有 PMS 或 pgvector 資料。

## Open Questions

- 正式部署時的模型 provider 與 connector 版本需在實作前鎖定；目前沿用既有 OpenAI-compatible 設定邊界。
- Conversation retention、使用者刪除歷史與管理者稽核存取可留給後續治理變更。
