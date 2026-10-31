## ADDED Requirements

### Requirement: Assistant selects tools through Semantic Kernel
The system SHALL use Semantic Kernel function calling to select among authorized specification, project, issue, and user tools from a single conversational message endpoint.

#### Scenario: User asks a specification question
- **WHEN** the user asks how a documented workflow or permission behaves
- **THEN** the assistant invokes specification retrieval and returns a grounded answer with source paths

#### Scenario: User requests project creation
- **WHEN** the user asks to create a project with sufficient information
- **THEN** the assistant invokes project preparation and returns a structured preview without creating persistent project data

### Requirement: Assistant supports missing-field follow-up
Tool validation SHALL return structured missing fields or ambiguity, and the assistant SHALL request the missing choice instead of inventing values or identifiers.

#### Scenario: Project name is missing
- **WHEN** the user requests project creation without a valid project name
- **THEN** the assistant asks for the project name and retains the pending conversational intent for the next message

#### Scenario: Multiple projects match a reference
- **WHEN** a tool returns multiple projects matching the user's wording
- **THEN** the assistant presents safe identifying choices and waits for the user to select one

### Requirement: Assistant can compose bounded tool calls
The system SHALL allow the model to select subsequent read or preparation tools using prior tool results within a bounded number of function-call round trips.

#### Scenario: Issue request requires project and user resolution
- **WHEN** a user asks to create an issue in a named project and assign it to a named person
- **THEN** the assistant resolves authorized project and user identifiers before preparing the issue preview

#### Scenario: Tool-call limit is reached
- **WHEN** the configured function-call round-trip limit is reached before a safe result is produced
- **THEN** the assistant stops execution, performs no mutation, and returns a recoverable error

### Requirement: Mutations require explicit confirmation
Kernel tools exposed for automatic selection MUST NOT directly create or modify PMS data; they SHALL produce pending action previews that are executed only by an authenticated confirmation operation using existing application services.

#### Scenario: User confirms a project or issue preview
- **WHEN** the owning user confirms a valid unexpired pending action and still has permission
- **THEN** the system executes it through the applicable service and records the structured result in the conversation

#### Scenario: Model claims an action succeeded without confirmation result
- **WHEN** generated text claims a mutation succeeded but no successful confirmation result exists
- **THEN** the client does not present the action as completed

### Requirement: Tool execution is authorized and observable
The system MUST evaluate current permissions for every tool invocation and SHALL record safe tool metadata without storing credentials or unnecessary sensitive content.

#### Scenario: User lacks permission for a tool
- **WHEN** the model requests a function the authenticated user is not permitted to use
- **THEN** the system refuses execution and returns a safe tool result to the assistant
