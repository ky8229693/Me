## MODIFIED Requirements

### Requirement: Project Issues use dedicated list and board views
The client SHALL display Project-scoped Issues on a single Project Issues route with explicit List and Board view controls. It SHALL preserve the selected view and supported search, type, status, priority, assignee, label, page, and page-size state in the URL query; pagination parameters SHALL apply only to List view.

#### Scenario: User applies Issue filters
- **WHEN** a user applies Issue search or filters in either Project Issues view
- **THEN** the client updates the active view and represents its view and filter state in the URL

#### Scenario: User switches to Board view
- **WHEN** a user selects Board from a filtered List view
- **THEN** the client retains compatible filters, removes inactive pagination semantics, and renders the Kanban on the same route

#### Scenario: User returns from Issue detail
- **WHEN** a user opens an Issue from a filtered list or board and returns to Project Issues
- **THEN** the previous view, filter, and applicable pagination state is restored

