## ADDED Requirements

### Requirement: Project overview summary access
The system SHALL provide an overview summary only to authenticated users who can access the requested Project through existing Project membership rules, and SHALL permit the same read-only summary for archived Projects.

#### Scenario: Project member reads overview
- **WHEN** an authenticated Project member requests the Project overview summary
- **THEN** the system returns summary data for only that Project

#### Scenario: Non-member requests overview
- **WHEN** an authenticated user without membership requests the Project overview summary
- **THEN** the system rejects the request using the existing inaccessible-Project behavior

#### Scenario: Member reads archived Project overview
- **WHEN** a Project member requests the overview of an archived Project
- **THEN** the system returns the read-only summary without enabling Project or Issue mutations

### Requirement: Project issue KPI summary
The system SHALL return total, InProgress, completed, and overdue Issue counts together with an integer completion percentage for the requested Project.

#### Scenario: Calculate populated Project KPIs
- **WHEN** a Project contains Issues in multiple workflow statuses and some non-Done Issues have due dates earlier than the query time
- **THEN** total counts every Project Issue, InProgress counts only `InProgress`, completed counts only `Done`, overdue counts past-due non-Done Issues, and completion percentage equals completed divided by total multiplied by 100

#### Scenario: Calculate empty Project KPIs
- **WHEN** a Project has no Issues
- **THEN** every KPI count and the completion percentage are returned as zero

#### Scenario: Completed past-due Issue is not overdue
- **WHEN** an Issue is `Done` and its due date is earlier than the query time
- **THEN** the Issue contributes to completed count but not overdue count

### Requirement: Workflow status distribution
The system SHALL return a count for every fixed Issue workflow status in workflow order, including statuses whose count is zero.

#### Scenario: Display complete status distribution
- **WHEN** the Project has no Issues in one or more workflow statuses
- **THEN** the overview still presents Draft, Approved, InProgress, QA, and Done with their corresponding counts

### Requirement: Recent and upcoming Project Issues
The system SHALL return at most five recently updated Issues and at most five upcoming incomplete Issues for the requested Project.

#### Scenario: Select recently updated Issues
- **WHEN** more than five Issues exist in the Project
- **THEN** recent Issues contain the five Issues with the latest update timestamps in descending order

#### Scenario: Select upcoming Issues
- **WHEN** incomplete Issues have due dates equal to or later than the query time
- **THEN** upcoming Issues contain at most the five nearest due dates in ascending order and exclude Done, overdue, and undated Issues

#### Scenario: Navigate from summary Issue
- **WHEN** a user activates an Issue in a recent or upcoming list
- **THEN** the frontend navigates to that Issue's detail page within the current Project

### Requirement: Responsive bilingual Project dashboard
The frontend SHALL present the Project overview KPI cards, completion progress, status distribution, recent Issues, upcoming Issues, existing Project information, and existing quick links using the active Traditional Chinese or English locale, and SHALL adapt the layout without horizontal page overflow.

#### Scenario: View overview in Traditional Chinese
- **WHEN** the active locale is Traditional Chinese and the user opens a Project Overview
- **THEN** dashboard headings, KPI labels, status labels, empty states, and error actions are displayed in Traditional Chinese

#### Scenario: View overview in English
- **WHEN** the active locale is English and the user opens a Project Overview
- **THEN** dashboard headings, KPI labels, status labels, empty states, and error actions are displayed in English

#### Scenario: View overview on a narrow viewport
- **WHEN** the Project Overview is rendered on a narrow viewport
- **THEN** cards and summary sections reflow within the workspace content width without causing horizontal page overflow

### Requirement: Project dashboard loading and failure feedback
The frontend SHALL distinguish loading, successful empty data, and request failure states for the Project overview summary.

#### Scenario: Summary is loading
- **WHEN** the overview summary request is pending
- **THEN** the frontend presents an accessible loading indicator instead of zero-valued KPI data

#### Scenario: Summary request fails
- **WHEN** the overview summary request fails
- **THEN** the frontend presents a localized error with a retry action and does not present the failure as an empty Project

