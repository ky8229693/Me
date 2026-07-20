## ADDED Requirements

### Requirement: Project Issues use a dedicated list route
The client SHALL display the Project-scoped Issue DataTable on the Project Issues route and SHALL preserve supported search, type, status, priority, assignee, page, and page-size state in the URL query.

#### Scenario: User applies Issue filters
- **WHEN** a user applies Issue search or filters in a Project workspace
- **THEN** the client updates the Issue list and represents the active list state in the URL

#### Scenario: User returns from Issue detail
- **WHEN** a user opens an Issue from a filtered page and returns to the Issue list
- **THEN** the previous filter and pagination state is restored

### Requirement: Issue details have addressable full pages
The client SHALL navigate an Issue selection to `/projects/{projectId}/issues/{issueId}` and SHALL render details, editable fields, assignee, allowed workflow transitions, comments, and history as full page content.

#### Scenario: User opens an Issue from the list
- **WHEN** a Project member selects an Issue title or Open action
- **THEN** the client navigates to the Issue's full-page URL

#### Scenario: User opens a shared Issue URL
- **WHEN** an authorized Project member directly opens a valid Issue URL
- **THEN** the client loads that Issue and its available details without first visiting the Project list

#### Scenario: Issue is outside accessible Project scope
- **WHEN** a user opens an Issue URL that the API does not expose within that Project
- **THEN** the client shows Not Found without leaking Issue content

### Requirement: Issue page provides contextual return navigation
The Issue detail page SHALL show a breadcrumb and an explicit Back to Issues action that returns to the containing Project's Issue list.

#### Scenario: User navigates back to Issues
- **WHEN** a user activates Back to Issues from Issue detail
- **THEN** the client returns to the containing Project's Issue list and restores list query state when available

### Requirement: Primary detail workflows do not use nested dialogs
The client SHALL NOT render Project detail or Issue detail as a Dialog and SHALL NOT open a Dialog from inside another Dialog. Dialogs SHALL be limited to short creation, confirmation, or selection operations.

#### Scenario: User creates an Issue from the list
- **WHEN** a user starts the short Issue creation operation
- **THEN** the client may use a single Dialog and navigates to or refreshes normal page content after completion

#### Scenario: User opens Issue history or comments
- **WHEN** a user reviews Issue history or comments
- **THEN** the content appears within the Issue detail page rather than another modal layer
