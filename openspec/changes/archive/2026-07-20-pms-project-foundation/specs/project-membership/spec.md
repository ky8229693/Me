## ADDED Requirements

### Requirement: Project membership uses fixed roles
The system SHALL assign each project membership exactly one of the fixed roles Owner, Manager, Member, or Viewer. A user SHALL have at most one active membership per project and MAY have memberships in multiple projects.

#### Scenario: User belongs to multiple projects
- **WHEN** an existing user is added to two different projects
- **THEN** the system stores an independent role for that user in each project

#### Scenario: Duplicate membership is requested
- **WHEN** an administrator attempts to add a user who is already a member of the project
- **THEN** the system rejects the duplicate membership without creating another record

#### Scenario: Read-only member views project roles
- **WHEN** a Member or Viewer opens the project member list
- **THEN** the system displays every member's assigned project role without presenting an editable control

### Requirement: Membership management follows project roles
The system SHALL allow an Owner to add an existing system user by email and assign any fixed project role. The system SHALL allow a Manager to add or manage only Member and Viewer memberships. Member and Viewer roles SHALL NOT manage memberships.

#### Scenario: Owner adds another Owner
- **WHEN** an Owner adds an existing system user to an active project with the Owner role
- **THEN** the system creates the membership and records an audit event

#### Scenario: Manager adds a Member
- **WHEN** a Manager adds an existing system user to an active project with the Member role
- **THEN** the system creates the membership and records an audit event

#### Scenario: Manager attempts to assign Owner
- **WHEN** a Manager attempts to add or promote a user to Owner
- **THEN** the system rejects the request and leaves memberships unchanged

#### Scenario: Member attempts to manage memberships
- **WHEN** a Member attempts to add, remove, or change another membership
- **THEN** the system rejects the request and leaves memberships unchanged

### Requirement: Authorized managers can search member candidates
The system SHALL allow an Owner or Manager of an active project to search enabled existing users by partial email after at least two characters are provided. The system SHALL return at most 10 matching users, exclude existing project members, and SHALL NOT permit Member, Viewer, non-member, or archived-project searches.

#### Scenario: Owner searches by partial email
- **WHEN** an Owner searches an active project with at least two email characters
- **THEN** the system returns up to 10 matching enabled non-member email addresses

#### Scenario: Existing member matches the search
- **WHEN** a matching user is already a member of the project
- **THEN** the system excludes that user from the candidate results

#### Scenario: Unauthorized user searches candidates
- **WHEN** a Member, Viewer, or non-member attempts to search project member candidates
- **THEN** the system rejects the request without returning account data

### Requirement: Every project retains an Owner
The system SHALL prevent any operation that would leave an active or archived project without at least one Owner.

#### Scenario: Remove the last Owner
- **WHEN** an authorized user attempts to remove or downgrade the project's last Owner
- **THEN** the system rejects the operation and preserves that Owner membership

#### Scenario: Remove one of multiple Owners
- **WHEN** an Owner removes another Owner while at least one Owner will remain
- **THEN** the system removes the target membership and records an audit event

### Requirement: Project access requires membership
The system SHALL evaluate project membership on the server for every project-scoped operation. Frontend visibility controls SHALL NOT replace API authorization.

#### Scenario: Member reads an accessible project
- **WHEN** an authenticated Member requests data from their project
- **THEN** the system authorizes the read according to the Member role

#### Scenario: Authenticated non-member calls a project API
- **WHEN** an authenticated user without project membership calls a project-scoped API
- **THEN** the system rejects the request without returning project data

### Requirement: Membership changes are auditable
The system SHALL record the actor, project, action, target user, previous role when applicable, new role when applicable, and timestamp for successful membership additions, role changes, and removals. Audit data SHALL NOT contain credentials, tokens, or secrets.

#### Scenario: Membership role changes
- **WHEN** an authorized user successfully changes a membership role
- **THEN** the system persists the role change and its audit event in one transaction

#### Scenario: Membership change fails
- **WHEN** a membership operation fails authorization or validation
- **THEN** the system does not persist a membership change or a successful-change audit event
