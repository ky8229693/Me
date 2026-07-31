## ADDED Requirements

### Requirement: Issue assignees must be current project members
The system SHALL allow Owner or Manager to assign an Issue to any current member of the same Project or clear the assignee. A Member creating an Issue SHALL leave it unassigned or assign it only to themselves. The system SHALL reject cross-Project, unknown, or removed users as assignee.

#### Scenario: Manager assigns a Project member
- **WHEN** a Manager assigns an Issue to a current member of the same Project
- **THEN** the system saves the assignee and records assignment history

#### Scenario: Member assigns another user during creation
- **WHEN** a Member creates an Issue and selects another user as assignee
- **THEN** the system rejects the request and creates no Issue

#### Scenario: Assignee is from another Project
- **WHEN** an authorized user selects a user who is not a member of the containing Project
- **THEN** the system rejects the assignment

### Requirement: Removing a project member clears active assignments
The system SHALL clear that user's assignee reference from Issues in the Project when their Project membership is removed and SHALL append attributable assignment history without deleting Issue or prior history.

#### Scenario: Assigned Member is removed from Project
- **WHEN** an Owner removes a Member who is assigned to one or more Project Issues
- **THEN** the system clears those assignments and records history in the membership-removal transaction

### Requirement: Project collaborators can add issue comments
The system SHALL allow Owner, Manager, and Member to add a non-empty comment up to 5000 characters to an Issue in an active Project. Viewer and non-member users SHALL NOT add comments. Comments SHALL identify their author and creation time.

#### Scenario: Member comments on an Issue
- **WHEN** a Member submits a valid comment to an Issue in their active Project
- **THEN** the system stores and returns the comment with author and timestamp

#### Scenario: Viewer attempts to comment
- **WHEN** a Viewer submits an Issue comment
- **THEN** the system rejects the request and stores no comment

#### Scenario: User comments in an archived Project
- **WHEN** an otherwise authorized user submits a comment to an archived Project Issue
- **THEN** the system rejects the mutation

### Requirement: Comments remain immutable in the foundation capability
The system SHALL allow Project members to read Issue comments in chronological order and SHALL NOT expose comment edit or delete operations in this capability.

#### Scenario: Member reads comments
- **WHEN** a Project member opens an Issue with comments
- **THEN** the system returns the comments oldest first with author identity and creation time

#### Scenario: User looks for comment mutation
- **WHEN** a client discovers the Issue foundation API
- **THEN** no comment update or deletion operation is available
