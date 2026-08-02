## ADDED Requirements

### Requirement: OpenSpec content is converted into a bounded vector index
The system SHALL load Markdown only from the configured OpenSpec root, SHALL split it into chunks with stable source metadata, and SHALL use the configured embedding model to build a bounded in-memory vector index.

#### Scenario: Index is built successfully
- **WHEN** an authenticated query requires semantic retrieval and no current index exists
- **THEN** the system embeds the allowed OpenSpec chunks in bounded batches and atomically publishes a complete index snapshot

#### Scenario: Bounded index includes multiple OpenSpec document categories
- **WHEN** eligible main specifications, active changes, and archived changes exceed the configured chunk limit
- **THEN** the system prioritizes main specifications before active changes and archived changes

#### Scenario: Concurrent queries require the first index
- **WHEN** multiple queries arrive while no index has been built
- **THEN** the system performs one shared index build and the queries use the same completed snapshot

#### Scenario: OpenSpec root is unavailable
- **WHEN** the configured OpenSpec root does not exist or contains no eligible Markdown chunks
- **THEN** the index reports an unavailable or empty state without reading files outside the configured root

### Requirement: Specification questions use semantic similarity
The system SHALL embed the user's validated question, calculate cosine similarity against the current index, and select at most the configured number of chunks whose scores meet the configured minimum similarity.

#### Scenario: Semantically related content uses different words
- **WHEN** a question and specification chunk are semantically related but do not share the same literal keywords
- **THEN** the embedding retriever can select that chunk according to cosine similarity

#### Scenario: No chunk reaches the similarity threshold
- **WHEN** every indexed chunk has a similarity score below the configured threshold
- **THEN** the system returns the existing no-supporting-specification response without calling the answer model

#### Scenario: Embedding vector is invalid
- **WHEN** an embedding response contains a zero vector, non-finite value, missing item, or inconsistent dimension
- **THEN** the system rejects the response and returns a safe service-unavailable result

### Requirement: Non-technical business questions use hybrid retrieval
The system SHALL support a Hybrid mode that combines semantic candidates with lexical candidates expanded from a controlled business vocabulary.

#### Scenario: User asks about project stages using non-technical wording
- **WHEN** a user asks「專案關卡有哪些」while the specification defines Issue workflow statuses
- **THEN** the retriever includes relevant `issue-workflow` content for the answer model

### Requirement: Embedding configuration supports local and remote providers
The system SHALL obtain the embedding base URL, model, optional API Key, timeout, batch size, and retrieval limits from server-side configuration and MUST NOT expose credentials to the client or logs.

#### Scenario: Remote provider uses a credential
- **WHEN** an API Key is configured for the embedding service
- **THEN** the backend sends it only in the server-to-server authorization header

#### Scenario: Local provider does not use a credential
- **WHEN** a local OpenAI-compatible endpoint and embedding model are configured without an API Key
- **THEN** the backend can call the endpoint without an authorization header

#### Scenario: Embedding model is not configured
- **WHEN** semantic retrieval is selected but its endpoint or model is missing
- **THEN** the assistant remains startable and reports embedding retrieval as unavailable when queried or rebuilt

### Requirement: Vector index status is observable and rebuildable
The system SHALL provide authenticated index status and rebuild operations without returning vectors, document content, or credentials.

#### Scenario: User reads index status
- **WHEN** an authenticated user requests specification index status
- **THEN** the system returns retrieval mode, lifecycle status, model, chunk count, and last successful build time

#### Scenario: Anonymous user accesses index operations
- **WHEN** an unauthenticated client requests index status or rebuild
- **THEN** the system rejects the request using the existing authentication behavior

#### Scenario: User rebuilds the index successfully
- **WHEN** an authenticated user requests a rebuild and all embeddings are valid
- **THEN** the system atomically replaces the previous snapshot and returns the new ready status

#### Scenario: Index rebuild fails
- **WHEN** an authenticated rebuild fails while a previous snapshot exists
- **THEN** the system returns a safe service-unavailable response and preserves the previous complete snapshot for subsequent queries

### Requirement: Client displays semantic retrieval state
The specification assistant page SHALL display the active retrieval mode and index lifecycle state and SHALL allow an authenticated user to request an index rebuild in Embedding mode.

#### Scenario: Embedding mode page is opened
- **WHEN** an authenticated user opens the specification assistant page while Embedding retrieval is configured
- **THEN** the page displays the embedding model, index state, chunk count, last build time, and rebuild control

#### Scenario: Index rebuild is in progress
- **WHEN** the user requests an index rebuild
- **THEN** the page disables duplicate rebuilds and displays a stable progress state

#### Scenario: Index status or rebuild fails
- **WHEN** the index API returns an error
- **THEN** the page displays a recoverable error without hiding the existing specification question interface

### Requirement: Specification answers use the application language
The answer model SHALL respond in Traditional Chinese regardless of the language detected in the question or reference text.

#### Scenario: Model could interpret Chinese text as Japanese
- **WHEN** the application asks the answer model a specification question containing CJK text
- **THEN** the system explicitly instructs the model to answer in Traditional Chinese
