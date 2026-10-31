## 1. PostgreSQL vector persistence foundation

- [x] 1.1 Add EF Core 8-compatible pgvector package, vector configuration options, and PostgreSQL vector registration
- [x] 1.2 Add specification vector entity, DbContext mapping, extension declaration, cosine HNSW index, and migration
- [x] 1.3 Add persistent index status and synchronization result contracts

## 2. Incremental synchronization and retrieval

- [x] 2.1 Implement a PostgreSQL vector store abstraction with compatible-row status and cosine TopK query
- [x] 2.2 Implement hash-based incremental synchronization with atomic insert/update/delete publication
- [x] 2.3 Add persistent semantic retriever and configure Memory versus PostgreSQL vector-store selection
- [x] 2.4 Preserve Hybrid lexical merge and safe unavailable behavior

## 3. API and client experience

- [x] 3.1 Change the index operation to incremental synchronization while preserving the existing authenticated route
- [x] 3.2 Extend safe index status with persistence and synchronization counts
- [x] 3.3 Show the synchronization control for Hybrid and Embedding modes and update localized UI copy

## 4. Verification and documentation

- [x] 4.1 Add unit tests for delta planning, unchanged chunks, model compatibility, and Hybrid persistent candidates
- [x] 4.2 Add PostgreSQL integration coverage for extension, persistence across service instances, cosine query, and stale deletion
- [x] 4.3 Update development documentation for pgvector setup, migration, sync, rollback, and model dimension constraints
- [x] 4.4 Run affected backend/frontend tests and builds, migration against local PostgreSQL, OpenSpec validation, and a restart persistence check
