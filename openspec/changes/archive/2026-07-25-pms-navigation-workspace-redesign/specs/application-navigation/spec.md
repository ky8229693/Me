## ADDED Requirements

### Requirement: Application navigation is URL based
The client SHALL use URL routes as the authoritative navigation state for Dashboard, Projects, system administration pages, Project workspace pages, and Issue detail pages. A supported route SHALL remain at the same logical page after browser refresh.

#### Scenario: User refreshes a routed page
- **WHEN** an authenticated user refreshes a supported Project or administration URL
- **THEN** the client restores the same logical page after session restoration

#### Scenario: User uses browser history
- **WHEN** a user navigates between supported pages and activates browser Back or Forward
- **THEN** the client follows browser history without relying on an in-memory active view

### Requirement: Sidebar groups features by domain
The client SHALL group visible navigation items under Workbench, Project Management, and System Administration. The client SHALL omit a group when none of its items are visible.

#### Scenario: Authorized administrator opens the application
- **WHEN** a user has permission to read users and roles
- **THEN** the sidebar shows Users and Roles under System Administration and Projects under Project Management

#### Scenario: Regular user opens the application
- **WHEN** a user lacks every system administration read permission
- **THEN** the sidebar omits the System Administration group

### Requirement: Administration navigation remains permission aware
The client SHALL show each system administration navigation item only when the authenticated user has its required permission. Hidden navigation SHALL NOT be treated as an authorization boundary.

#### Scenario: User lacks role read permission
- **WHEN** an authenticated user does not have `roles.read`
- **THEN** the Role Management navigation item is not rendered

#### Scenario: User manually enters an unauthorized administration route
- **WHEN** a user opens an administration URL without its required permission
- **THEN** the client prevents rendering the protected page and the API remains the security boundary

### Requirement: Routed pages provide orientation and failure states
The client SHALL show page headings and contextual breadcrumbs for nested resources and SHALL provide understandable Not Found, Forbidden, loading, and general error states.

#### Scenario: User opens an unknown route
- **WHEN** a user navigates to a URL that is not registered
- **THEN** the client shows a Not Found page with a route back to a known location

#### Scenario: Project context is loading
- **WHEN** a Project route is resolving its Project data
- **THEN** the workspace shows a stable loading state without displaying another Project's content

### Requirement: Production hosting supports routed refreshes
The deployment documentation SHALL require non-API application routes to fall back to the Vue `index.html`, while `/api` requests continue to target the backend.

#### Scenario: User directly requests a nested frontend URL
- **WHEN** the production web host receives a request for `/projects/{projectId}/issues`
- **THEN** it serves the SPA entry point so Vue Router can resolve the route
