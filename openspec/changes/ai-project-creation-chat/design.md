## Context

規格助理已有驗證保護的問答端點、OpenAI-compatible client 與 Vue 頁面；PMS 已有受權限政策保護的專案建立端點與 `ProjectAdministrationService`。本變更跨越 LLM、API、權限與 UI，且必須避免模型輸出直接造成資料異動。

## Goals / Non-Goals

**Goals:**

- 在同一個規格助理頁面提供簡化的對話式專案建立入口。
- 將自然語言解析成受限的名稱與描述，先預覽、後確認。
- 確認時重新套用既有 `Projects.Create` 權限及專案服務驗證。
- 草稿綁定建立者、限時且只能成功使用一次。

**Non-Goals:**

- 自主 Agent、多輪記憶、Issue 批次建立或任意工具選擇。
- 讓 LLM 直接操作 EF Core 或資料庫。
- 第一版導入 Semantic Kernel 套件；先以小型介面驗證工作流，未來可替換 orchestration 實作。

## Decisions

### 1. 使用獨立 draft 與 confirm endpoints

`POST /api/spec-assistant/project-drafts` 只解析並儲存草稿，`POST /api/spec-assistant/project-drafts/{id}/confirm` 才執行建立。相較讓 query endpoint 同時讀寫，此設計使副作用、授權與測試邊界明確。

### 2. LLM 僅輸出固定 JSON 合約

擴充 assistant client 解析 `{ name, description }`，使用低 temperature、嚴格 JSON 驗證與長度限制。模型不取得資料庫、HTTP 工具或任意程式執行能力。

### 3. 草稿使用伺服器記憶體快取

第一版以 `IMemoryCache` 保存十五分鐘、綁定 user id 的一次性草稿，不新增資料表。確認成功後移除；驗證失敗時保留，方便使用者修正流程。多節點與重啟持久化留待後續需求。

### 4. 沿用既有 ProjectAdministrationService

確認操作呼叫現有 `CreateAsync`，讓名稱、描述、Owner membership、transaction 與 audit 行為保持一致。兩個 endpoints 都要求 `Projects.Create` permission。

### 5. UI 先提供明確模式切換

聊天室輸入區提供「詢問規格」與「建立專案草稿」兩個動作，避免第一版依賴不透明的 intent routing。草稿以卡片顯示並要求確認，成功後提供建立結果。

## Risks / Trade-offs

- [模型可能輸出不合法 JSON 或臆造欄位] → 僅接受固定欄位、嚴格反序列化並回傳可理解的錯誤，不建立資料。
- [重複確認造成重複專案] → 以原子方式取出草稿，第一次確認後立即失效。
- [記憶體草稿在重啟或多節點遺失] → 第一版接受短期草稿特性；需要水平擴展時改用分散式快取。
- [提示注入要求繞過權限] → LLM 不持有工具；API 每次確認都以登入身分重新授權。
- [未立即使用 Semantic Kernel] → 保留解析與工作流服務邊界，等工具數量與多步驟狀態增加後再導入，避免過早增加框架成本。
