## ADDED Requirements

### Requirement: Issue representations include Project labels
Issue list and detail responses SHALL include the Issue's assigned Project labels with stable identifiers, names, and display colors, and Issue create or update responses SHALL return the authoritative label set.

#### Scenario: Member reads a labeled Issue
- **WHEN** a Project member requests an Issue with assigned labels
- **THEN** the response includes only labels belonging to the containing Project

### Requirement: Issue queries support label filtering
Project Issue list and board queries SHALL accept a label filter and SHALL return only Issues assigned every requested label. Unknown or cross-Project label identifiers SHALL produce a structured validation error.

#### Scenario: User filters by two labels
- **WHEN** a Project member requests Issues using two valid Project label identifiers
- **THEN** the result contains only Issues assigned both labels

