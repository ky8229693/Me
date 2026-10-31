## 1. Shared document and embedding contracts

- [x] 1.1 Extract secure OpenSpec Markdown discovery and stable chunk generation into a shared document source used by both retrievers
- [x] 1.2 Add embedding configuration, vector/index models, status contracts, and replaceable embedding/index interfaces
- [x] 1.3 Register retrieval mode selection, embedding HTTP client, singleton vector index, and retrievers without requiring a live provider at startup

## 2. Embedding and vector retrieval

- [x] 2.1 Implement the OpenAI-compatible embeddings client with batching, optional Bearer authentication, cancellation, and strict response validation
- [x] 2.2 Implement finite-vector validation and cosine similarity with dimension and zero-vector safeguards
- [x] 2.3 Implement concurrency-safe lazy index build and atomic rebuild while preserving the last valid snapshot on failure
- [x] 2.4 Implement embedding retrieval with query embedding, TopK ranking, minimum similarity threshold, bounded context, and normalized sources
- [x] 2.5 Refactor lexical retrieval to use the shared document source and add the configured retrieval facade without silent fallback

## 3. Index API and frontend experience

- [x] 3.1 Add authenticated index status and rebuild endpoints with safe unavailable responses and no vector/content/credential exposure
- [x] 3.2 Add frontend index status/rebuild API types and centralized client calls
- [x] 3.3 Extend the specification assistant page with localized retrieval mode, model, lifecycle, chunk count, build time, rebuild progress, and recoverable errors

## 4. Verification and learning documentation

- [x] 4.1 Add backend unit tests for chunk stability, embedding request/validation, cosine similarity, semantic ranking, threshold behavior, and concurrent atomic rebuilds
- [x] 4.2 Add backend integration tests for index authentication, status, successful rebuild, safe failure, and unchanged query response contract
- [x] 4.3 Add frontend API and view tests for ready, unbuilt, rebuilding, successful rebuild, and failure states
- [x] 4.4 Document Embedding versus generation model configuration, Ollama-compatible examples, index lifecycle, and lexical fallback usage
- [x] 4.5 Run affected backend tests/build, frontend tests/build, OpenSpec validation, and targeted E2E locally or CI according to risk

## 5. Retrieval quality corrections

- [x] 5.1 Prioritize main specifications before active and archived change documents in the bounded index
- [x] 5.2 Require the answer model to respond in Traditional Chinese
- [x] 5.3 Add regression coverage and run affected backend tests/build and OpenSpec validation

## 6. Non-technical business queries

- [x] 6.1 Add controlled business-term expansion for project stages and user management
- [x] 6.2 Add Hybrid retrieval mode that merges lexical and semantic candidates
- [x] 6.3 Verify the retrieval regression tests, API build, and OpenSpec validation
