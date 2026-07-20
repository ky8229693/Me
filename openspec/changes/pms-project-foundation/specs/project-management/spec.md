## ADDED Requirements

### Requirement: Authorized users can create projects
The system SHALL allow an authenticated user with the system-level `Projects.Create` permission to create a project with a name and optional description. The system SHALL create the project and the creator's Owner membership atomically.

#### Scenario: Authorized user creates a project
- **WHEN** an authenticated user with `Projects.Create` submits valid project data
- **THEN** the system creates an active project and assigns that user as its Owner

#### Scenario: User without permission attempts to create a project
- **WHEN** an authenticated user without `Projects.Create` submits project data
- **THEN** the system rejects the request with the appropriate forbidden status and creates no project or membership

### Requirement: Users only discover accessible projects
The system SHALL return projects only when the authenticated user has an active membership in the project. A system role alone SHALL NOT grant visibility into every project.

#### Scenario: User lists projects
- **WHEN** an authenticated user requests the project list
- **THEN** the system returns only projects in which the user has an active membership

#### Scenario: Non-member requests a project
- **WHEN** an authenticated user requests a project for which the user has no membership
- **THEN** the system does not disclose the project content

### Requirement: Authorized project members can update project details
The system SHALL allow an Owner or Manager to update the name and description of an active project. Member and Viewer roles SHALL NOT update project details.

#### Scenario: Manager updates an active project
- **WHEN** a Manager submits valid changes to an active project
- **THEN** the system saves the changes and records an audit event

#### Scenario: Viewer attempts to update a project
- **WHEN** a Viewer submits changes to a project
- **THEN** the system rejects the request and preserves the existing project data

### Requirement: Projects use an archive lifecycle
The system SHALL support Active and Archived project states and SHALL NOT provide hard deletion in this capability. Only an Owner SHALL archive or restore a project.

#### Scenario: Owner archives a project
- **WHEN** an Owner archives an active project
- **THEN** the system marks the project as archived, preserves its memberships and history, and records an audit event

#### Scenario: Archived project receives a mutation
- **WHEN** a user attempts a project or membership mutation on an archived project other than restoring it
- **THEN** the system rejects the mutation and preserves existing data

#### Scenario: Owner restores an archived project
- **WHEN** an Owner restores an archived project
- **THEN** the system marks the project as active and records an audit event

### Requirement: Project APIs preserve the shared API contract
The system SHALL expose project operations under `/api/projects`, use the shared `ApiResponse<T>` envelope, and preserve appropriate HTTP status codes and centralized exception handling.

#### Scenario: Project request succeeds
- **WHEN** a caller completes a valid project operation
- **THEN** the API returns the result in the shared response envelope with the appropriate success status

#### Scenario: Project request fails validation
- **WHEN** a caller submits invalid project data
- **THEN** the API returns a structured error in the shared response envelope with the appropriate client error status


