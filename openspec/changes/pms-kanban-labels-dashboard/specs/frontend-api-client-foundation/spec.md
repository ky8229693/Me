## MODIFIED Requirements

### Requirement: Frontend API types are reusable
The frontend SHALL define reusable TypeScript types for shared API responses, Dashboard data, Project labels, and Issue list, board, and detail data used by the client.

#### Scenario: UI imports PMS data types
- **WHEN** a Dashboard or Issue UI component needs a backend response shape
- **THEN** it MUST import the reusable type from the API layer instead of declaring a duplicate local type

## ADDED Requirements

### Requirement: PMS API calls are isolated from UI components
The frontend SHALL expose Dashboard, Project label, and Issue query or mutation functions from dedicated API modules instead of calling `fetch` directly from Vue components.

#### Scenario: Dashboard loads work data
- **WHEN** Dashboard requests its authenticated workbench model
- **THEN** it MUST call a dedicated API module through the shared API client

## REMOVED Requirements

### Requirement: Todo API calls are isolated from UI components
**Reason**: Todo UI and backend endpoints are removed from the product.

**Migration**: Delete the Todo API module; new PMS modules use the shared API client.

### Requirement: Existing Todo user behavior is preserved
**Reason**: Todo is no longer an application capability.

**Migration**: Dashboard becomes the authenticated PMS workbench.

