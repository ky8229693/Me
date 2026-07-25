## ADDED Requirements

### Requirement: Issues follow the fixed workflow
The system SHALL support only Draft, Approved, InProgress, QA, and Done statuses. New Issues SHALL start in Draft and status changes SHALL use only the explicitly allowed transitions.

#### Scenario: New Issue starts in Draft
- **WHEN** an authorized collaborator creates an Issue
- **THEN** the system stores the Issue with Draft status regardless of client-supplied status

#### Scenario: User attempts to skip a workflow state
- **WHEN** a user attempts to transition an Issue from Draft directly to InProgress
- **THEN** the system rejects the transition and preserves Draft status

### Requirement: Approval and completion require project management authority
The system SHALL allow only Project Owner or Manager to transition Draft to Approved, QA to Done, or Done to InProgress.

#### Scenario: Manager approves a Draft
- **WHEN** a Manager transitions an Issue from Draft to Approved
- **THEN** the system saves Approved status and appends a status history entry

#### Scenario: Assigned Member attempts to complete QA
- **WHEN** an assigned Member attempts to transition an Issue from QA to Done
- **THEN** the system rejects the transition and preserves QA status

### Requirement: Assignees can advance and return active work
The system SHALL allow Owner, Manager, or the current assignee to transition Approved to InProgress, InProgress to QA, and QA to InProgress. An unassigned Member or a Member who is not the current assignee SHALL NOT execute these transitions.

#### Scenario: Assignee starts approved work
- **WHEN** the current assignee transitions an Approved Issue to InProgress
- **THEN** the system saves InProgress status and appends a status history entry

#### Scenario: Non-assignee Member submits work to QA
- **WHEN** a Member who is not the current assignee attempts to transition an InProgress Issue to QA
- **THEN** the system rejects the transition

### Requirement: Archived projects freeze issue workflow
The system SHALL reject every Issue status transition when the containing Project is Archived.

#### Scenario: Owner transitions an archived Project Issue
- **WHEN** an Owner attempts an otherwise valid transition in an archived Project
- **THEN** the system rejects the transition and preserves Issue status

### Requirement: Issue history is append-only and attributable
The system SHALL append immutable history entries for Issue creation and every successful status, assignee, type, priority, title, description, and due-date change. Each entry SHALL identify the actor, change type, affected field when applicable, previous value, new value, and timestamp. Failed operations SHALL NOT append successful-change history.

#### Scenario: Assignee changes
- **WHEN** an authorized Manager assigns an Issue to a different Project member
- **THEN** the system saves the assignment and its history entry in one transaction

#### Scenario: Unauthorized update fails
- **WHEN** an unauthorized user attempts to update an Issue
- **THEN** the system preserves the Issue and appends no successful-change history


