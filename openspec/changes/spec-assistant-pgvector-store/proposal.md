## Why

第二階段的向量索引只存在 API 記憶體，應用重啟後必須重新計算全部規格 Embedding，規格異動也只能整批重建。專案已使用本機 PostgreSQL，現在加入 pgvector 可練習可持久化、可增量同步的 RAG 索引，並降低重啟與更新成本。

## What Changes

- 在既有 PostgreSQL 啟用 pgvector，新增規格 chunk、內容 hash、模型、維度與向量的持久化資料表。
- 以內容 hash 比較 OpenSpec chunks，只嵌入新增或變更內容，並刪除已不存在的索引資料。
- 將 Hybrid retrieval 的語意查詢改由 PostgreSQL cosine distance 執行，保留字面檢索與業務詞彙展開。
- API 啟動後直接重用相同模型的既有索引，不再因 process restart 整批重建。
- 將「重建索引」管理操作改為「同步規格」，並回報新增、更新、刪除與未變更數量。
- 增加 pgvector availability、增量同步、模型切換、持久化查詢與安全失敗測試。

## Capabilities

### New Capabilities

- `spec-vector-persistence`: 定義 pgvector schema、持久化語意查詢、hash-based 增量同步與索引生命週期。

### Modified Capabilities

<!-- 第二階段 capability 尚未封存；第三階段維持既有 query contract，索引管理語意由 rebuild 演進為 sync。 -->

## Impact

- Backend：新增 pgvector .NET mapping、EF Core entity/migration、persistent index repository 與同步服務。
- Database：`web_demo` 需要 `vector` extension，新增 1024 維 `bge-m3` 向量欄位與 cosine index/query。
- API：保留既有路徑以避免破壞前端，rebuild endpoint 內部改為增量同步並擴充安全狀態 metadata。
- Frontend：索引操作文案與狀態改為同步規格。
- Configuration：新增向量儲存模式與 embedding dimension；模型或維度改變時需建立相容的新快照。
- Tests：單元測試不依賴真實 Ollama；pgvector 整合驗證使用本機或 CI PostgreSQL。
