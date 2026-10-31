## ADDED Requirements

### Requirement: Specification embeddings persist in PostgreSQL
The system SHALL store eligible OpenSpec chunks and their embeddings in PostgreSQL pgvector with stable identity, source metadata, content hash, embedding model, dimension, and update time.

#### Scenario: API restarts after a successful sync
- **WHEN** the API restarts with compatible model and dimension configuration
- **THEN** semantic queries reuse the persisted vectors without embedding every specification chunk again

#### Scenario: pgvector is unavailable
- **WHEN** the configured PostgreSQL database does not provide the vector extension or schema
- **THEN** the assistant reports a safe unavailable state without exposing connection details

### Requirement: Specification synchronization is incremental
The system SHALL compare current stable chunks with persisted rows and SHALL embed only new or content-changed chunks while removing rows whose chunks no longer exist.

#### Scenario: One specification section changes
- **WHEN** a sync finds one changed chunk and all other chunks retain the same IDs and hashes
- **THEN** the system embeds and updates only the changed chunk and reports the remaining chunks as unchanged

#### Scenario: A specification is deleted
- **WHEN** persisted chunks for the current model no longer exist in the OpenSpec source
- **THEN** the sync deletes those stale rows without re-embedding unchanged chunks

#### Scenario: Sync fails before publication
- **WHEN** document loading or embedding a changed chunk fails
- **THEN** the system preserves the last complete persisted index and reports a safe synchronization failure

### Requirement: Semantic retrieval executes in pgvector
The system SHALL embed the validated question and use PostgreSQL cosine distance to return at most TopK compatible chunks meeting the configured similarity threshold.

#### Scenario: Hybrid query uses persistent semantic candidates
- **WHEN** Hybrid retrieval receives a business question
- **THEN** it merges pgvector semantic candidates with lexical and controlled-vocabulary candidates before grounding the answer model

#### Scenario: Persisted vectors use another model or dimension
- **WHEN** rows are incompatible with the configured embedding model or dimension
- **THEN** semantic retrieval excludes those rows and reports that synchronization is required when no compatible index exists

### Requirement: Client can observe and synchronize the persistent index
The authenticated index API and specification assistant page SHALL expose persistent index status and an explicit synchronization operation without exposing vectors, document content, credentials, or connection strings.

#### Scenario: Hybrid page is opened
- **WHEN** an authenticated user opens the assistant in Hybrid mode
- **THEN** the page displays the persistent model, compatible chunk count, last sync time, and a「同步規格」control

#### Scenario: Synchronization succeeds
- **WHEN** the user requests synchronization
- **THEN** the page reports inserted, updated, deleted, and unchanged counts and remains usable for questions
