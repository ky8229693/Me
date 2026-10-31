## ADDED Requirements

### Requirement: Project managers maintain Project-scoped labels
The system SHALL allow an Owner or Manager of an active Project to create, rename, recolor, and delete labels belonging only to that Project. A label SHALL have a trimmed unique name of 1 to 40 characters and a validated display color. Member, Viewer, non-member, and archived-Project mutations SHALL be rejected.

#### Scenario: Manager creates a label
- **WHEN** a Manager submits a valid name and color not already used by name in the Project
- **THEN** the system creates the Project label and returns it

#### Scenario: Duplicate label name is submitted
- **WHEN** an Owner or Manager submits a label name matching another Project label case-insensitively
- **THEN** the system returns a structured validation conflict and preserves existing labels

#### Scenario: Viewer edits labels
- **WHEN** a Viewer attempts to create, update, or delete a label
- **THEN** the system rejects the mutation

### Requirement: Issue labels follow Issue edit authorization
The system SHALL allow labels from the containing Project to be assigned to or removed from an Issue only by a user who may edit that Issue under existing role, creator, status, and Project archive rules. Labels from another Project SHALL be rejected.

#### Scenario: Manager assigns Project labels
- **WHEN** a Manager updates an active Project Issue with valid labels from that Project
- **THEN** the system replaces the Issue label set and records the label change in Issue history

#### Scenario: Cross-Project label is submitted
- **WHEN** an Issue update references a label belonging to another Project
- **THEN** the system rejects the update without changing Issue labels

### Requirement: Label deletion detaches existing assignments
Deleting a Project label SHALL remove its assignments from all Issues in that Project without deleting the Issues or their history, and SHALL append an auditable label-removal history entry for affected Issues.

#### Scenario: Manager deletes an assigned label
- **WHEN** a Manager deletes a label currently assigned to one or more Issues
- **THEN** the label is removed from those Issues and each affected Issue retains an audit record of the change

