## Context

API 已使用 EF Core 8 與 Npgsql 連接 PostgreSQL，第二階段以 singleton `InMemorySpecVectorIndex` 保存 OpenSpec chunks 與 `bge-m3` 1024 維向量。索引在 process restart 後消失，且 rebuild 會重新呼叫全部 Embedding。開發機 PostgreSQL 18 已安裝官方 pgvector 0.8.2。

## Goals / Non-Goals

**Goals:**

- 將規格向量持久化於既有 PostgreSQL，重啟後可直接查詢。
- 使用 stable chunk ID 與 content hash 進行新增、更新、刪除的增量同步。
- 在 PostgreSQL 執行 cosine distance TopK 查詢，維持 Hybrid lexical merge。
- 模型或維度不相容時不混用舊向量，並提供明確同步狀態。
- 維持既有 query 與 index endpoint 路徑，降低前端遷移成本。

**Non-Goals:**

- 多租戶向量庫、跨節點同步、背景 queue、檔案 watcher。
- 大規模 ANN tuning、reranker、LangChain 或外部 vector service。
- 將既有 PMS 業務資料庫搬遷到其他 database provider。

## Decisions

### 1. 使用既有 PostgreSQL 與官方 pgvector .NET integration

使用 `Pgvector.EntityFrameworkCore` 的 EF Core 8 相容版本 0.2.2，`AppDbContext` 宣告 vector extension、1024 維欄位及 cosine HNSW index。相較另建 Docker vector database，此方案可重用現有連線、migration、備份與測試方式。

### 2. 以 chunk ID + model 作為持久化身分

資料列保存 stable chunk ID、capability、source path、content、content hash、embedding model、dimension、embedding 與 updated time。唯一鍵使用 `(ChunkId, EmbeddingModel)`；查詢與同步只使用目前模型及維度，避免不同向量空間混合。

### 3. 同步採用完整清單比較、只嵌入 delta

同步載入目前 bounded OpenSpec chunks，與資料庫同模型資料比較：不存在者 insert、hash 改變者 update、來源已移除者 delete、其餘 unchanged。只對 insert/update chunks 分批呼叫 Embedding，所有變更在 transaction 內發布。

### 4. PostgreSQL 查詢 cosine distance

問題向量由現有 `IEmbeddingClient` 產生，repository 以 `CosineDistance` 排序、套用 `1 - distance >= MinimumSimilarity` 與 TopK。Hybrid facade 仍與 lexical 結果合併，業務詞彙展開保持不變。

### 5. 保留 endpoint 路徑但將操作語意改為同步

`POST /api/spec-assistant/index/rebuild` 暫時保留相容性，內部執行增量 sync；status 顯示 persistent store、model、chunk count、last sync 與同步統計。前端按鈕改為「同步規格」，Hybrid 與 Embedding 都顯示。

## Risks / Trade-offs

- [pgvector extension 未安裝] → migration/查詢回傳安全 unavailable，文件提供本機與 CI 安裝方式。
- [固定 `vector(1024)` 綁定 bge-m3] → dimension 設定必須為 1024；更換不同維度模型需 migration 或新欄位策略。
- [同步 transaction 期間 Embedding 很慢] → Embedding 在 transaction 前完成，transaction 只包含資料庫 delta publication。
- [小資料集 HNSW 收益有限] → 保留 index 作為第三階段學習，exact results 仍受 TopK 與 threshold 控制。
- [第三階段與未 commit 的第二階段重疊] → 保持 change 分離但實作建立在目前工作樹，提交時依階段分開。

## Migration Plan

1. 確認 PostgreSQL `vector` extension 可用。
2. 加入 package、entity mapping 與 EF migration。
3. 套用 migration，第一次 sync 建立 `bge-m3` vectors。
4. 切換 DI 至 persistent index/retriever，更新 UI 文案。
5. 回復時可切回 `VectorStore=Memory` 使用第二階段索引；資料表可保留，不影響業務資料。

## Open Questions

- 未來若改用非 1024 維模型，採多維度分表或 migration 由後續 change 決定。
