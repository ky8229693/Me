## ADDED Requirements

### Requirement: Projects open as full workspace pages
The client SHALL navigate from the Project list to a full Project workspace route instead of opening Project details in a Dialog.

#### Scenario: User opens a Project
- **WHEN** a user selects a Project from the Project list
- **THEN** the client navigates to that Project's workspace URL and the Project list is no longer the page background

### Requirement: Project workspace provides contextual navigation
The Project workspace SHALL provide a stable header with Project name, lifecycle status, current user's Project role, breadcrumb, and navigation for Overview, Issues, Members, and authorized Settings.

#### Scenario: Project member changes workspace section
- **WHEN** a Project member selects Issues or Members in the Project navigation
- **THEN** the client changes the nested route while preserving the same Project context

#### Scenario: Member without management authority views navigation
- **WHEN** a Member or Viewer opens the Project workspace
- **THEN** the client provides Overview, Issues, and Members but omits the management Settings item

### Requirement: Project workspace respects membership boundaries
The client SHALL load Project context through the existing Project API and SHALL NOT infer Project access from system roles. A Project response that does not expose the resource SHALL produce a Not Found workspace state.

#### Scenario: System Admin without membership opens Project URL
- **WHEN** a System Admin directly opens a Project workspace URL for a Project where they are not a member
- **THEN** the client shows Not Found without rendering Project content

### Requirement: Archived Project state is visible throughout the workspace
The Project workspace SHALL visibly identify an Archived Project and SHALL render its child pages consistently with existing read-only API capabilities.

#### Scenario: Member browses an archived Project
- **WHEN** a Project member opens any workspace section for an Archived Project
- **THEN** the workspace displays the archived state and does not present unavailable mutation actions

### Requirement: Project management uses dedicated workspace pages
The client SHALL present member administration and Project settings as page content rather than Project-detail Dialog content. Owner-only and Owner-or-Manager actions SHALL remain differentiated according to existing behavior.

#### Scenario: Manager opens Project settings
- **WHEN** a Manager opens the Settings route for an active Project
- **THEN** the page permits supported Project edits but does not present Owner-only archive or restore actions

#### Scenario: Owner opens Members
- **WHEN** an Owner opens the Members route
- **THEN** the page presents membership search, role management, and removal operations permitted by the API
