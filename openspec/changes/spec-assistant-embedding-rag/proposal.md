## Why

第一階段的字面關鍵字檢索無法可靠理解同義詞、跨語言問法或不同術語，例如「工作項目」與 `Issue`。第二階段以 Embedding 與向量相似度建立真正的語意檢索練習，提升規格助理找到正確依據的能力，同時維持現有 API 與 WEB UI 合約。

## What Changes

- 新增 OpenAI-compatible Embeddings API client，可設定獨立的 endpoint、模型與 API Key，並能在未來連接 Ollama。
- 將 OpenSpec Markdown 切成具有穩定識別碼與來源中繼資料的 chunks，建立記憶體向量索引。
- 查詢時對問題產生 Embedding，以 cosine similarity 選取超過門檻的候選段落，再交給第一階段 LLM client 回答。
- 支援啟動後延遲建立索引、併發查詢共用同一次建置，以及規格檔變更後的明確重建操作。
- 在現有規格助理頁面顯示檢索模式與索引狀態，並提供已驗證使用者重建索引的操作。
- 保留 lexical retriever 作為可設定的開發 fallback，但預設使用 Embedding；Embedding 服務不可用時回傳清楚且安全的服務狀態。
- 新增語意檢索、向量計算、索引生命週期、API 及前端狀態測試。
- 新增 Hybrid 模式，透過受控業務詞彙展開合併字面與向量結果，支援非開發者用語。

## Capabilities

### New Capabilities

- `spec-embedding-retrieval`: 定義 OpenSpec chunk embedding、向量索引、語意查詢、索引狀態與重建行為。

### Modified Capabilities

<!-- 第一階段 spec-assistant change 尚未封存；其對外問答合約保持不變。 -->

## Impact

- Backend：新增 embedding options/client、向量索引與 cosine similarity retriever，並擴充規格助理狀態／重建端點。
- Frontend：擴充規格助理 API client 與頁面，顯示 Embedding 模式、索引狀態及重建操作。
- Configuration：新增 Embedding endpoint、模型、API Key、相似度門檻與索引限制；可與生成模型使用不同服務。
- Runtime：索引保存在記憶體，應用重啟後重新建立；本階段不加入外部向量資料庫。
- Tests：不依賴真實模型，使用確定性 fake embeddings 驗證語意排序與完整流程。
