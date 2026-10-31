## ADDED Requirements

### Requirement: Project Issues have a workflow board
The client SHALL provide a Project-scoped Kanban view with one column for each fixed workflow status in Draft, Approved, InProgress, QA, and Done order. Each card SHALL show title, priority, assignee, due-date state, and labels and SHALL link to the Issue detail page.

#### Scenario: Member opens the board
- **WHEN** a Project member selects Board on the Project Issues page
- **THEN** the client displays accessible Issues grouped by their current workflow status

#### Scenario: User opens a board card
- **WHEN** a user selects an Issue card
- **THEN** the client navigates to that Issue's addressable detail page

### Requirement: Board status moves enforce existing workflow authorization
The system SHALL accept a board status move only when the requested target appears in the Issue's allowed workflow transitions for the current user. The client SHALL disable or reject unavailable moves and SHALL restore the authoritative state when a move fails.

#### Scenario: Assignee advances permitted work
- **WHEN** an assigned Member moves an Issue to a permitted next status
- **THEN** the system updates the status, appends Issue history, and places the card in the target column

#### Scenario: User attempts a forbidden move
- **WHEN** a user attempts a status move not allowed by workflow or authorization rules
- **THEN** the system preserves the previous status and the client restores the card with an understandable error

### Requirement: Board supports the Issue filter set
The board SHALL support search, type, priority, assignee, and label filters within the current Project. It SHALL load all matching cards needed for the fixed columns without using DataTable pagination and SHALL apply a bounded result limit with an explicit refinement state when the limit is exceeded.

#### Scenario: User filters board cards
- **WHEN** a Project member applies assignee and label filters in Board view
- **THEN** every column displays only matching Issues and the active filters are represented in the URL query

#### Scenario: Board result exceeds its bound
- **WHEN** matching Issues exceed the configured board result limit
- **THEN** the client explains that results are incomplete and prompts the user to refine filters

