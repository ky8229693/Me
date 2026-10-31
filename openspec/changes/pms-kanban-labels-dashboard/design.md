## Context

The application already persists Projects, memberships, Issues, Issue history/comments, Identity roles, and permission mappings in PostgreSQL. Issue workflow transitions and resource authorization are authoritative on the API. The Project Issues client currently provides a paginated DataTable and full-page detail route, while Dashboard is an early Todo demonstration backed by a singleton in-memory service.

This change crosses persistence, Minimal API endpoints, resource authorization, query models, Vue route state, responsive UI, and all three test layers. It must preserve the rule that Project membership is the only Project data boundary and that System Admin has no implicit cross-Project access.

## Goals / Non-Goals

**Goals:**

- Provide a practical List/Board Issue workspace without duplicating workflow rules in the client.
- Add reusable Project-scoped labels with auditable Issue assignment changes.
- Replace the Todo Dashboard with a bounded, actionable, server-derived PMS workbench.
- Remove Todo code and tests completely rather than leaving disabled routes or dead abstractions.
- Keep filter and view state addressable through Vue Router query parameters.
- Preserve build, unit, integration, E2E, and OpenSpec validation.

**Non-Goals:**

- Custom workflow columns, WIP limits, arbitrary card ordering, swimlanes, or real-time collaboration.
- Global labels shared between Projects.
- Charts requiring a new visualization dependency or historical trend aggregation.
- Notifications, attachments, subtasks, time tracking, saved personal filters, or offline support.
- Pinia, a drag-and-drop package, background workers, or a new cache layer.

## Decisions

### 1. Persist labels as Project-owned entities with an explicit many-to-many join

Add `ProjectLabel` with `Id`, `ProjectId`, `Name`, normalized name, color, and timestamps, plus an explicit `IssueLabel` join keyed by Issue and label. A unique index on Project plus normalized name enforces case-insensitive uniqueness. Foreign keys ensure an Issue and label relationship is queryable and cleanup is deterministic; the service additionally verifies both belong to the same Project before mutation.

An explicit join is preferred over a string/JSON array because it supports filtering, referential integrity, rename without mass Issue updates, and future label reporting. Labels are Project-scoped rather than global to preserve the established resource boundary and avoid a new system-level authorization model.

Deleting a label uses an application service transaction: load affected Issue IDs, append audit history entries, remove joins, then delete the label. Database cascade alone is insufficient because it cannot produce the required Issue history.

### 2. Reuse Issue edit and transition authorization as the only mutation authority

Label assignment goes through `IssueAdministrationService` and the same `IssueAccessService` decision used for editable Issue fields. Board moves call the existing workflow transition operation; the client does not infer permission from Project role alone. Issue responses continue to expose server-computed `allowedTransitions` and edit capabilities.

This avoids a separate Kanban authorization path that could drift from Issue detail behavior. The trade-off is that each card must carry sufficient action metadata or fetch authoritative detail when an action occurs.

### 3. Use a dedicated bounded board read endpoint

Keep the paginated Issue list endpoint for DataTable behavior and add a Project-scoped board query endpoint using the same normalized filter object. The board response returns matching card projections grouped or tagged by status, with a configurable hard maximum initially set to 500 and an `isTruncated`/total indication.

Repeatedly paging the existing list endpoint would create race conditions, extra requests, and inconsistent totals. Returning an unlimited Project dataset would create an unbounded query and DOM. A dedicated bounded projection also avoids loading comments, history, or full descriptions.

### 4. Keep one Project Issues route and encode view state in query

`/projects/:projectId/issues` remains the canonical route. Query `view=list|board` selects presentation; compatible filters (`search`, `type`, `status`, `priority`, `assignee`, `labels`) survive view changes. `page` and `pageSize` are ignored and removed when Board becomes active. The return target stored when opening Issue detail includes the complete query.

One route is preferred to a separate `/board` route because List and Board represent the same resource and filter context. It also minimizes contextual navigation and breadcrumb changes.

### 5. Provide accessible status movement in addition to pointer drag

Cards may use native pointer/HTML drag behavior without adding a runtime dependency, but each actionable card also exposes a keyboard-accessible PrimeVue action for the server-provided allowed transitions. Both interactions invoke the same transition function, show pending state, and refresh or roll back to the authoritative board response on failure.

This avoids making drag-and-drop the only control and avoids adopting a package for a fixed five-column workflow. Mobile layouts use horizontal board scrolling and the explicit transition action instead of depending on drag precision.

### 6. Build Dashboard as one authenticated read model endpoint

Add `GET /api/dashboard` returning a bounded `DashboardResponse`: summary counts, assigned non-Done Issue cards, overdue Issue cards, status counts, and recent activity items. The query starts from current Project memberships, excludes archived Projects for actionable counts, and joins Project names into each result. Recent activity may combine `IssueHistory` and `ProjectAuditEvent`, sorted descending and capped per response.

A single endpoint provides a consistent snapshot, avoids client request fan-out, and centralizes membership filtering. It remains a read model in a focused service rather than introducing CQRS infrastructure. Counts and lists are calculated on request initially; database indexes and projections will be added for assignee/status/due-date/history access.

### 7. Remove Todo vertically across the repository

Remove Todo endpoint mapping, DI registration, service interface/implementation, contracts, frontend API/type references, Vue components, Dashboard logic, xUnit/Vitest/Playwright scenarios, and obsolete documentation. No database migration is required because Todo data is process memory and intentionally has no persistence or ownership.

Requests to `/api/todos` will naturally return 404. No compatibility endpoint or data migration is provided because the feature is explicitly a disposable demo and has no durable user data.

### 8. Keep feature boundaries pragmatic

New backend code is grouped under Dashboard and Issues/Labels as feature-specific endpoints, contracts, and services while existing files are changed incrementally. This change will not reorganize all existing Endpoints, Models, and Services. Frontend API calls remain centralized in `api/`, with board/label UI under `features/issues` and Dashboard composition in its view or focused child components.

## Risks / Trade-offs

- [A Project can exceed the board bound] → Return truncation metadata, show an explicit refinement message, and retain the paginated List for complete access.
- [Native drag behavior varies across touch devices] → Treat explicit transition actions as fully supported behavior and drag as an enhancement.
- [Dashboard aggregation becomes slow as data grows] → Use projection-only queries, bounded lists, `AsNoTracking`, and supporting indexes; measure before adding caches or materialized summaries.
- [Label deletion creates many history rows] → Perform it transactionally and keep label deletion restricted to Project managers; large-scale bulk/background deletion is out of scope.
- [Optimistic UI can disagree with server authorization] → Disable actions while pending and reload authoritative board state after success or failure.
- [Removing Todo breaks old bookmarks and tests] → Treat 404 as intentional, remove all advertised navigation/docs, and replace coverage in the same change.
- [Combining four product changes enlarges review scope] → Implement in vertical slices with independently testable backend and frontend tasks, then remove Todo only after the replacement Dashboard is functional.

## Migration Plan

1. Add label entities, indexes, relationships, and EF Core migration; deploy additive schema first.
2. Implement label management/assignment and board/dashboard read endpoints with authorization and integration tests.
3. Add frontend label controls, List/Board switch, board interactions, and Dashboard workbench.
4. Replace Todo tests with PMS scenarios, then remove Todo backend/frontend code and documentation.
5. Run migration, backend tests, frontend unit/build, E2E, OpenSpec validation, and production container build checks.

Rollback before label data is relied upon can revert application code and migration. After production labels exist, rollback should retain the additive label tables until a later controlled cleanup rather than dropping user data automatically. Todo removal has no data rollback requirement; restoring the old code would create a fresh in-memory list only.

## Open Questions

- None blocking. Initial board maximum is an implementation configuration value (default 500) and may be tuned from measured usage without changing capability semantics.
