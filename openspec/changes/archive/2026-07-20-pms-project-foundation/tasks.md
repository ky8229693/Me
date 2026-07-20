## 1. Data Model and Permissions

- [x] 1.1 Add Project, ProjectMembership, fixed ProjectRole, ProjectStatus, and ProjectAuditEvent data models with required constraints and timestamps
- [x] 1.2 Register project entities, relationships, unique membership constraint, and indexes in AppDbContext
- [x] 1.3 Add the system-level project permissions required by this change to the existing permission catalog and initializer
- [x] 1.4 Create and review the EF Core migration for project, membership, and audit tables

## 2. Project Authorization and Services

- [x] 2.1 Implement resource-based authorization that resolves project membership and fixed project-role capabilities
- [x] 2.2 Implement project creation with atomic first-Owner membership and audit event
- [x] 2.3 Implement membership-filtered project list and project detail queries without implicit System Admin bypass
- [x] 2.4 Implement active-project updates for Owner and Manager with audit recording
- [x] 2.5 Implement Owner-only archive and restore behavior and centralized archived-project mutation protection
- [x] 2.6 Implement Owner and Manager membership operations, duplicate prevention, role restrictions, and last-Owner protection

## 3. Project API

- [x] 3.1 Add project request and response contracts with validation and shared ApiResponse behavior
- [x] 3.2 Add `/api/projects` Minimal API endpoints for create, list, detail, update, archive, and restore operations
- [x] 3.3 Add nested project membership endpoints for listing, adding, changing roles, and removing members
- [x] 3.4 Register project services, authorization handlers, policies, and endpoint module wiring in Program.cs
- [x] 3.5 Add Swagger tags and operation metadata for project and membership endpoints

## 4. Backend Verification

- [x] 4.1 Add unit tests for project lifecycle, membership role rules, duplicate membership, and last-Owner protection
- [x] 4.2 Add authorization tests covering member access, non-member denial, fixed role capabilities, and absence of implicit Admin access
- [x] 4.3 Add API integration tests for successful project and membership flows, validation failures, forbidden operations, and archived-project behavior
- [x] 4.4 Run backend build and complete backend test suite

## 5. Frontend Project Experience

- [x] 5.1 Add typed project and membership API clients using the shared HTTP client
- [x] 5.2 Add project navigation and a membership-filtered project list view
- [x] 5.3 Add project creation and project detail editing UI with permission-aware actions
- [x] 5.4 Add project member list and membership management UI for Owner and Manager capabilities
- [x] 5.5 Add archived-project read-only presentation and Owner archive/restore actions

## 6. Frontend and End-to-End Verification

- [x] 6.1 Add frontend unit tests for project API clients, project views, role-based actions, and archived state
- [x] 6.2 Add E2E coverage for project creation, member assignment, access isolation, role restrictions, archive, and restore
- [x] 6.3 Run frontend build, unit tests, and relevant E2E tests
- [x] 6.4 Run the full solution test suite and OpenSpec validation

## 7. Member Candidate Autocomplete

- [x] 7.1 Add an Owner/Manager-only member candidate API with minimum search length, result limit, enabled-account filtering, and existing-member exclusion
- [x] 7.2 Replace free-form member email input with PrimeVue autocomplete while retaining server-side add validation
- [x] 7.3 Add backend, frontend, and E2E regression coverage for partial-email suggestions and access restrictions
- [x] 7.4 Run relevant tests, frontend build, and OpenSpec validation

## 8. Read-only Role Presentation

- [x] 8.1 Display fixed role labels instead of disabled selectors whenever the current user cannot manage a membership row
- [x] 8.2 Add a frontend regression test for Member/Viewer visibility of Owner and Manager roles
- [x] 8.3 Run relevant frontend tests, build, and OpenSpec validation
