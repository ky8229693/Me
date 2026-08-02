## Context

第一階段以 `ISpecDocumentRetriever` 隔離檢索實作，現有 `LexicalSpecDocumentRetriever` 每次查詢掃描 OpenSpec Markdown 並以文字命中排序。問答 API、LLM client 與 Vue 頁面已具備，因此第二階段可保持外部合約，將檢索替換為 Embedding RAG。使用者預計日後自建 Ollama，所以生成模型與 Embedding 必須可分開設定，且不得綁定雲端 SDK。

## Goals / Non-Goals

**Goals:**

- 透過 OpenAI-compatible `/embeddings` API 對規格 chunks 與問題建立向量。
- 使用 cosine similarity 找出語意相關內容，改善同義詞與跨語言問題。
- 建立執行程序內共用的記憶體索引，提供狀態與明確重建操作。
- 保持 `/api/spec-assistant/query` 的第一階段合約與可信來源保證。
- 讓 Embedding client、文件來源、向量索引與 retriever 可獨立測試及替換。
- 支援無 API Key 的本機 OpenAI-compatible endpoint，同時不降低遠端 credential 安全性。

**Non-Goals:**

- pgvector、Qdrant、Pinecone、Chroma 或其他持久化向量資料庫。
- 自動檔案監看、分散式索引同步、增量 upsert 或大量文件最佳化。
- 通用 reranker、對話記憶、Agent 或 LangChain／Semantic Kernel。
- Embedding 模型下載、Ollama 安裝與基礎設施部署。

## Decisions

### 1. 將文件載入與 chunking 從 retriever 分離

新增 `ISpecDocumentSource`，只負責安全掃描允許的 OpenSpec 根目錄、產生穩定 chunk ID、capability、相對來源路徑與內容。Lexical 與 Embedding retriever 共用此來源，避免兩種模式產生不同引用。穩定 ID 由相對路徑、段落序號與內容 hash 組合，利於日後增量索引。

### 2. 使用 OpenAI-compatible Embeddings HTTP contract

新增 `IEmbeddingClient`，一次傳送 bounded batch 的 chunk 文字到 `{BaseUrl}/embeddings`，解析回應中依 index 排序的浮點向量。設定與生成模型分開，包含 `BaseUrl`、`Model`、選用的 `ApiKey`、timeout 與 batch size；有 Key 時送 Bearer header，未提供時允許呼叫本機服務。

選擇直接 HTTP 而非供應商 SDK，是為了同時支援雲端服務與 Ollama，並保留第三階段導入框架時的比較價值。

### 3. Singleton 記憶體索引搭配 lazy build

`ISpecVectorIndex` 保存 chunks、向量、建立時間、狀態與最近錯誤。首次 Embedding 查詢以 `SemaphoreSlim` 保證只有一次索引建置；後續查詢重用向量。重建會先在暫存集合完整建立成功，再原子替換現有 snapshot，避免查詢看到半成品。

應用啟動不強制連線 Embedding 服務，避免未設定 Ollama 或雲端 API 時阻止其他功能啟動。

### 4. Cosine similarity 與明確門檻

查詢向量與索引中每個 chunk 計算 cosine similarity，依分數降冪選取 `TopK`，並排除低於 `MinimumSimilarity` 的結果。零向量、維度不一致與非有限數值視為 embedding 錯誤，不進入 LLM prompt。

門檻與 TopK 為設定值，預設值以測試與練習安全範圍開始；第三階段前再以固定問題集評估品質。

### 5. 模式、狀態與重建 API

新增已驗證端點：

- `GET /api/spec-assistant/index`：回傳檢索模式、索引狀態、chunk 數、模型及建立時間，不回傳 Key 或向量。
- `POST /api/spec-assistant/index/rebuild`：觸發並等待安全重建，成功回傳新狀態，失敗回傳 503 且保留舊 snapshot。

頁面顯示目前模式與索引狀態，Embedding 模式提供重建按鈕。第一階段 query endpoint 在索引／Embedding 服務不可用時沿用安全 503 行為。

### 6. 以設定選擇 retriever

`SpecAssistant:RetrievalMode` 接受 `Hybrid`、`Embedding` 或 `Lexical`，預設 `Hybrid`。DI 註冊同一個 `ISpecDocumentRetriever` facade，依設定委派；Lexical 作為離線 UI 驗證與故障診斷選項，而不是在 Embedding 失敗時靜默降級，避免使用者誤以為正在做語意搜尋。

## Risks / Trade-offs

### 正式規格優先與固定繁體中文回答

文件來源在 chunk 上限內依序收錄 `openspec/specs`、進行中的 changes、封存 changes，避免歷史文件先占滿索引。生成提示詞固定要求繁體中文（zh-TW），避免小型模型將短 CJK 問句誤判為日文。

### Hybrid 檢索與業務詞彙展開

`Hybrid` 模式同時執行 Embedding 與 lexical retrieval，優先保留字面候選後合併語意候選。受控詞彙表將「專案關卡」等非技術用語展開為規格中的 Issue、狀態與工作流程術語；詞彙只影響檢索，不直接產生答案。

- [記憶體索引在重啟後消失] → lazy rebuild，並在 UI 顯示尚未建立狀態；持久化留給後續階段。
- [大型規格索引造成大量 API 請求] → bounded chunks、batch embedding、內容與 chunk 數限制。
- [不同模型向量維度不同] → snapshot 記錄模型，重建時驗證每個向量維度一致；設定變更需重建。
- [相似度門檻不適合所有模型] → 將門檻外部化並加入分數測試；不把分數視為跨模型可比較值。
- [免費／本機服務暫時不可用] → 安全 503、保留舊 snapshot、提供可重試的重建操作。
- [規格內容可能送往第三方] → 文件只送到使用者明確設定的 Embedding endpoint，文件資料政策須由部署者評估；本機 Ollama 可避免外送。
- [多人同時重建] → Semaphore 合併併發建置，查詢持續使用完整舊 snapshot，直到新 snapshot 成功替換。

## Migration Plan

1. 抽取共用文件來源並保持 lexical 測試通過。
2. 加入 Embedding client、vector index 與 retriever，預設設定不含模型及 Key。
3. 加入狀態／重建 API 與前端狀態卡。
4. 使用 fake embedding 完成自動測試；實際環境設定 Ollama 或供應商後手動建立索引。
5. 回復時將 `RetrievalMode` 設為 `Lexical` 即可，不涉及資料 migration。

## Open Questions

- Ollama 最終採用的 embedding 模型（例如專用多語 embedding 模型）將在使用者安裝時決定；實作只要求 OpenAI-compatible contract。
- 是否持久化向量及採用哪一種 vector store，留到規格數量或啟動成本足以證明需要時再決定。
