## ADDED Requirements

### Requirement: Dashboard and Kanban have component test coverage
The frontend SHALL include component tests for Dashboard loading states and navigation, Issue List/Board view state, filters, card navigation, and permitted or rejected status moves without calling the real backend.

#### Scenario: Dashboard data loads
- **WHEN** Dashboard is mounted with a successful mocked workbench response
- **THEN** tests verify summaries and actionable Issue links are rendered

#### Scenario: Board move fails
- **WHEN** a mocked status transition rejects a board move
- **THEN** tests verify the card returns to its authoritative column and an error is shown

## REMOVED Requirements

### Requirement: Todo UI has component test coverage
**Reason**: Todo components and workflow are removed.

**Migration**: Replace Todo component coverage with Dashboard and Kanban interaction coverage.

