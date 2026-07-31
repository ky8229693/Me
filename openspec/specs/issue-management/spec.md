# Issue Management Specification

## Purpose

定義 Project Issue 的建立、驗證、存取、更新、查詢與刪除限制。

## Requirements

### Requirement: Project collaborators can create typed issues
The system SHALL allow an Owner, Manager, or Member of an active project to create an Issue with type Feature, Bug, or ChangeRequest; priority Low, Medium, High, or Critical; a title; optional description; optional due date; and an optional permitted assignee. Every new Issue SHALL start in Draft status and record its creator and timestamps. Viewer and non-member users SHALL NOT create Issues.

#### Scenario: Member creates a Bug
- **WHEN** a Member submits a valid Bug in an active project without another user as assignee
- **THEN** the system creates a Draft Issue linked to that Project and records its creator

#### Scenario: Viewer attempts to create an Issue
- **WHEN** a Viewer submits an Issue creation request
- **THEN** the system rejects the request and creates no Issue or history

#### Scenario: User creates an Issue in an archived Project
- **WHEN** any user attempts to create an Issue in an archived Project
- **THEN** the system rejects the mutation and preserves existing data

### Requirement: Issue fields follow validation rules
The system SHALL require a trimmed title between 2 and 200 characters, limit description to 10000 characters, accept only known type and priority values, and reject an invalid due date representation.

#### Scenario: Issue title is invalid
- **WHEN** a user submits a blank or overlength Issue title
- **THEN** the system returns a structured validation error and creates no Issue

#### Scenario: Unknown Issue type is submitted
- **WHEN** a user submits an Issue type outside Feature, Bug, and ChangeRequest
- **THEN** the system returns a structured validation error

### Requirement: Project members only discover issues in accessible projects
The system SHALL allow a Project member to read Issues, details, history, and comments in that Project. The system SHALL NOT expose Issue existence or content to authenticated non-members, including System Admin users without Project membership.

#### Scenario: Member lists Project Issues
- **WHEN** a Project member requests the Issue list
- **THEN** the system returns only Issues belonging to that Project

#### Scenario: Admin without membership requests an Issue
- **WHEN** a System Admin without Project membership requests an Issue detail
- **THEN** the system returns Not Found without Issue content

### Requirement: Issue updates follow role and creator rules
The system SHALL allow Owner and Manager to update any Issue fields in an active Project. A Member SHALL update the editable fields of an Issue only when the Issue is in Draft and the Member created it. Viewer SHALL NOT update Issues.

#### Scenario: Member edits own Draft
- **WHEN** a Member updates the title or description of a Draft Issue they created
- **THEN** the system saves the change and appends the field changes to Issue history

#### Scenario: Member edits another creator's Draft
- **WHEN** a Member attempts to update a Draft Issue created by another user
- **THEN** the system rejects the update and preserves the Issue

### Requirement: Issue lists support search filters and pagination
The system SHALL provide Project-scoped Issue pagination with default page size 20 and maximum page size 100, ordered by most recently updated first. The system SHALL support case-insensitive title or description search and filters for type, status, priority, and assignee.

#### Scenario: User filters open Bugs assigned to a member
- **WHEN** a Project member requests Issues filtered by Bug type, non-Done status, and an assignee
- **THEN** the system returns the matching page and total count within that Project

#### Scenario: User submits invalid pagination
- **WHEN** page is less than 1 or page size exceeds 100
- **THEN** the system returns a structured validation error

### Requirement: Issues are not deleted in the foundation capability
The system SHALL NOT expose hard-delete or soft-delete Issue operations in this capability.

#### Scenario: User looks for Issue deletion
- **WHEN** a client discovers the Issue foundation API
- **THEN** no Issue deletion operation is available
