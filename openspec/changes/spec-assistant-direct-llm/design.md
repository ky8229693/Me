## Context

專案以 ASP.NET Core Minimal API、Vue 3 與 OpenSpec Markdown 管理需求。主規格位於 `openspec/specs`，進行中的變更位於 `openspec/changes`；目前沒有 LLM client、文件索引或向量儲存。第一階段的目的在於練習最小的可追溯問答流程，同時保留日後以 Embedding/RAG 替換檢索器的界面。

## Goals / Non-Goals

**Goals:**

- 提供經驗證使用者可操作的規格問答 API 與 Vue 頁面。
- 以可設定的 OpenAI-compatible Chat Completions HTTP API 直接呼叫模型。
- 使用確定性文字檢索限制傳給模型的規格內容，並回傳可驗證的來源路徑。
- 讓檢索器及 LLM client 可在測試中替換，且後續能獨立升級為向量檢索或工作流框架。
- 避免 API Key、完整 Prompt 或敏感 Header 出現在前端與應用程式紀錄。

**Non-Goals:**

- Embedding、向量資料庫、語意重新排序與 RAG 評測。
- LangChain、Semantic Kernel、Agent、工具呼叫或多輪對話記憶。
- 自動修改規格、建立 change，或將 OpenSpec 文件暴露為任意檔案下載 API。
- 串流回覆、用量計費介面或多家供應商的完整抽象。

## Decisions

### 1. 使用小型內部介面隔離檢索與模型呼叫

端點只依賴 `ISpecDocumentRetriever` 與 `ISpecAssistantClient`。第一階段的 retriever 掃描允許的 OpenSpec 根目錄、切分 Markdown 段落，依正規化 token 命中數排序並限制候選數量。這讓第二階段可以替換為 embedding retriever，而不改變 API 合約或頁面。

替代方案是直接把整個 `openspec` 目錄送給模型；其 token 成本、延遲與無關內容都會隨文件成長，且不利於測試，因此不採用。

### 2. 直接用具名 HttpClient 呼叫 OpenAI-compatible Chat Completions

伺服器設定包含 `BaseUrl`、`Model`、`ApiKey`、逾時與候選內容限制。API Key 僅從伺服器設定／環境變數取得。直接 HTTP 呼叫能清楚呈現第一階段的請求、回應和錯誤處理，也避免先引入工作流框架；模型服務由 mock handler 進行測試。

替代方案是立即使用 LangChain.NET 或 Semantic Kernel，但會掩蓋基礎串接細節並超出本階段學習範圍。

### 3. 回答合約包含結構化來源，後端掌握來源真實性

Prompt 要求模型只依據候選內容回答、無依據時明確說明，並把規格內容標示為不可信資料以降低 prompt injection 風險。最終 `sources` 與 `relatedSpecs` 由後端依實際候選文件建立，不採信模型自行產生的檔案路徑。回應沿用 `ApiResponse<T>` 並保留適當 HTTP 狀態碼。

### 4. 限制文件範圍與輸入大小

文件載入器只接受設定好的專案 OpenSpec 根目錄及 `.md` 文件，回傳相對路徑，拒絕路徑跳脫。問題必須非空白且有長度上限；候選段落數與總字元數皆受設定限制。第一版不監看檔案，採每次查詢載入或短生命週期快取，優先保持實作透明。

### 5. 規格助理屬於已登入工作台功能

API 要求驗證但第一階段不新增細分 permission；前端在 Workbench 顯示入口並使用既有 auth session、router 與 API client。頁面提供問題輸入、送出／錯誤／空結果狀態，以及答案、相關規格和來源清單。

## Risks / Trade-offs

- [文字比對無法理解同義詞] → 在 UI 說明第一階段限制，建立 retriever 介面並於第二階段加入 Embedding。
- [外部模型逾時、限流或設定缺漏] → 設定明確逾時，轉換為一致的 503 回應，不暴露上游內容或 API Key。
- [規格內容造成 prompt injection] → 將文件視為資料、使用固定系統指令、限制能力為唯讀問答，來源由後端產生。
- [每次掃描文件增加延遲] → 限制掃描範圍與內容大小；只有觀察到需要時才加入快取。
- [OpenAI-compatible 實作仍可能有欄位差異] → 第一階段明確支援 Chat Completions 基本欄位，其他供應商差異留給後續適配器。
- [開發者未設定 API Key] → 應用仍可啟動，查詢時回傳可理解的服務未設定狀態，測試不依賴真實金鑰。

## Migration Plan

1. 加入後端設定與服務，但不在版控中放入 API Key。
2. 加入 API、前端入口及測試；未設定模型時既有功能維持正常。
3. 由開發者透過 user secrets 或環境變數設定模型服務後進行目標 E2E 驗證。
4. 如需回復，移除前端入口與端點註冊即可，不涉及資料庫 migration 或既有資料轉換。

## Open Questions

- 實際練習要連接 OpenAI、Azure OpenAI 或本機 OpenAI-compatible 服務；設計先以設定支援切換，實作與自動測試不綁定真實供應商。
- 第一階段的品質基準將先使用固定問題測試候選選取；完整 RAG 評測資料集留到第二階段。
