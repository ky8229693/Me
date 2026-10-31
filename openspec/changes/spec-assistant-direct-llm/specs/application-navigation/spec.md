## MODIFIED Requirements

### Requirement: Sidebar groups features by domain
The client SHALL group visible navigation items under Workbench, Project Management, and System Administration. The client SHALL show the specification assistant under Workbench for authenticated users and SHALL omit a group when none of its items are visible.

#### Scenario: Authorized administrator opens the application
- **WHEN** a user has permission to read users and roles
- **THEN** the sidebar shows the specification assistant under Workbench, Users and Roles under System Administration, and Projects under Project Management

#### Scenario: Regular user opens the application
- **WHEN** an authenticated user lacks every system administration read permission
- **THEN** the sidebar shows the specification assistant under Workbench and omits the System Administration group
