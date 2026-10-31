## ADDED Requirements

### Requirement: Dashboard summarizes the current user's accessible work
The authenticated Dashboard SHALL show counts for Issues assigned to the current user, overdue non-Done Issues assigned to the user, and accessible Issues grouped by workflow status. Calculations SHALL include only active Projects where the user is a current member and SHALL use the server's current UTC time for overdue evaluation.

#### Scenario: User opens Dashboard
- **WHEN** an authenticated user opens Dashboard
- **THEN** the client displays server-derived work and status summaries limited to the user's accessible active Projects

#### Scenario: User has no accessible work
- **WHEN** an authenticated user has no accessible active Projects or matching Issues
- **THEN** Dashboard displays zero and empty states without exposing other Projects

### Requirement: Dashboard provides actionable work lists
The Dashboard SHALL show bounded lists of the current user's assigned non-Done Issues, overdue Issues, and recent accessible Project activity ordered by relevance or recency. Each item SHALL identify its Project and link to the corresponding Project or Issue page.

#### Scenario: User follows assigned work
- **WHEN** a user selects an assigned Issue on Dashboard
- **THEN** the client navigates directly to the Issue detail route

#### Scenario: Recent activity is outside membership
- **WHEN** activity belongs to a Project where the user is not a current member
- **THEN** the Dashboard response excludes that activity

### Requirement: Dashboard handles partial and failed loading clearly
The client SHALL provide stable loading, empty, and error states and SHALL allow the user to retry Dashboard loading without reloading the application.

#### Scenario: Dashboard request fails
- **WHEN** the Dashboard API cannot be loaded
- **THEN** the client shows an understandable error and a retry action while preserving application navigation

