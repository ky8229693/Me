## ADDED Requirements

### Requirement: Authenticated users can ask specification questions
The system SHALL allow an authenticated user to submit a non-empty natural-language question to the specification assistant and SHALL return the result in the shared API response envelope.

#### Scenario: Authenticated user asks a valid question
- **WHEN** an authenticated user submits a question within the configured length limit
- **THEN** the system returns an answer together with related specification identifiers and source paths

#### Scenario: Anonymous user asks a question
- **WHEN** an unauthenticated client submits a specification question
- **THEN** the system rejects the request using the existing authentication behavior

#### Scenario: User submits an invalid question
- **WHEN** an authenticated user submits a blank question or one exceeding the supported length
- **THEN** the system returns a validation error without calling the language model

### Requirement: Answers are grounded in selected OpenSpec content
The system MUST select candidate content only from allowed OpenSpec Markdown files and SHALL instruct the language model to answer only from that content.

#### Scenario: Relevant specification content exists
- **WHEN** lexical retrieval identifies content relevant to the user's question
- **THEN** the system sends only the bounded candidate content to the configured language model

#### Scenario: No supporting specification content exists
- **WHEN** retrieval finds no content with sufficient textual relevance
- **THEN** the system responds that the current specifications do not define the requested information and does not invent a source

### Requirement: Source references are trustworthy
The system SHALL derive related specification identifiers and normalized source paths from the files actually selected by the backend and MUST NOT trust model-generated paths as source metadata.

#### Scenario: Model returns a grounded answer
- **WHEN** the language model completes a question using selected candidate documents
- **THEN** the response source list contains only normalized project-relative paths for those candidate documents

#### Scenario: OpenSpec content contains conflicting instructions
- **WHEN** a selected Markdown document contains text attempting to change assistant behavior or fabricate a source
- **THEN** the system continues treating document text as reference data and derives sources from backend metadata

### Requirement: Language model configuration remains server side
The system MUST obtain the language model base URL, model name, and credential from server-side configuration and MUST NOT expose the credential to the browser or application logs.

#### Scenario: Language model is configured
- **WHEN** a valid model configuration and credential are available
- **THEN** the backend calls the configured OpenAI-compatible endpoint using the configured model

#### Scenario: Language model is not configured
- **WHEN** an authenticated user asks a question while required model configuration is missing
- **THEN** the system returns a service-unavailable response with a safe explanatory message

#### Scenario: Upstream model request fails
- **WHEN** the configured model service times out, rate limits, or returns an invalid response
- **THEN** the system returns a safe service-unavailable response without disclosing credentials or raw upstream data

### Requirement: Specification assistant page presents complete query states
The client SHALL provide an authenticated specification assistant page with question entry, submission progress, answer, related specifications, sources, empty-result guidance, and recoverable error states.

#### Scenario: User receives an answer
- **WHEN** an authenticated user submits a valid question and the API succeeds
- **THEN** the page displays the answer and its related specifications and sources

#### Scenario: Query is in progress
- **WHEN** a submitted specification question is awaiting a response
- **THEN** the page prevents duplicate submission and displays a stable loading state

#### Scenario: Query fails
- **WHEN** the specification assistant API returns an error
- **THEN** the page displays an understandable error and allows the user to retry
