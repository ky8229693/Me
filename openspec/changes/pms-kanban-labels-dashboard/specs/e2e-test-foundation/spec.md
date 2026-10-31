## ADDED Requirements

### Requirement: PMS workbench primary flow test
E2E suite SHALL verify that an authenticated Project member can open Dashboard, follow an assigned Issue, return to the Project Issues view, apply a label filter, and use a permitted Kanban workflow transition.

#### Scenario: User follows Dashboard work into Kanban
- **WHEN** an authenticated test user opens an assigned Issue from Dashboard and returns to its Project Board
- **THEN** the Issue is visible under its current status and matching label filters

#### Scenario: User advances permitted Issue
- **WHEN** the test user performs an allowed Kanban transition
- **THEN** the card appears in the target column and remains there after page reload

## REMOVED Requirements

### Requirement: Todo 主要流程測試
**Reason**: Todo 功能、UI 與 API 已移除。

**Migration**: 以 Dashboard、Issue labels 與 Kanban 的主要 PMS 工作路徑取代。

