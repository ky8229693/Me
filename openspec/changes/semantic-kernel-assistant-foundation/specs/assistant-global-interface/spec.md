## ADDED Requirements

### Requirement: Assistant is available from the authenticated application shell
The authenticated application SHALL provide an assistant trigger in the Topbar that opens a right-side Drawer on desktop and a full-width assistant panel on narrow screens.

#### Scenario: User opens the assistant from a project page
- **WHEN** the user activates the Topbar assistant trigger while viewing an accessible project
- **THEN** the assistant opens without navigating away and visibly offers the current project as context

#### Scenario: User closes and reopens the Drawer
- **WHEN** the user closes the assistant Drawer and later reopens it
- **THEN** the active conversation and any pending action remain available

### Requirement: Drawer and Workspace share conversations
The Drawer and full Assistant Workspace SHALL use the same conversation identifiers, messages, context, and pending actions.

#### Scenario: User moves a conversation to the Workspace
- **WHEN** the user selects the full-workspace action from the Drawer
- **THEN** the application opens the same conversation in the Assistant Workspace without losing its state

### Requirement: Interface distinguishes assistant states
The interface SHALL distinguish ordinary messages, tool activity, missing-information requests, pending previews, failures, and confirmed results using structured API state rather than model prose alone.

#### Scenario: Write action requires confirmation
- **WHEN** the assistant prepares a project or issue action
- **THEN** the interface renders its validated preview with explicit confirm and cancel controls

#### Scenario: Long content is produced in the Drawer
- **WHEN** an answer or workflow is unsuitable for the Drawer width
- **THEN** the interface provides an option to continue the same conversation in the full Workspace

### Requirement: Assistant interface is accessible and responsive
The global assistant SHALL support keyboard operation, focus management, accessible labels, and responsive sizing without obscuring primary application controls.

#### Scenario: Drawer opens with keyboard navigation
- **WHEN** a keyboard user opens and closes the assistant
- **THEN** focus moves into the panel and returns to the trigger according to dialog accessibility behavior
