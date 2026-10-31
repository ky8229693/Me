## 1. Backend overview read model

- [x] 1.1 Add Project overview response contracts for KPIs, workflow counts, recent Issues, and upcoming Issues.
- [x] 1.2 Implement the membership-scoped Project overview query with fixed KPI and ordering rules.
- [x] 1.3 Expose `GET /api/projects/{projectId}/overview` through the existing endpoint structure and shared API response envelope.
- [x] 1.4 Add backend tests for authorization, archived Projects, empty/populated KPI calculations, fixed status distribution, and five-item Issue limits/order.

## 2. Frontend dashboard experience

- [x] 2.1 Add typed API client support for retrieving a Project overview summary.
- [x] 2.2 Rebuild Project Overview with responsive KPI cards, completion progress, workflow distribution, recent Issues, upcoming Issues, existing Project information, and quick links.
- [x] 2.3 Add explicit loading, empty, failure, retry, and Issue-detail navigation behavior.
- [x] 2.4 Add Traditional Chinese and English translations for every new Project dashboard label and state.

## 3. Automated verification

- [x] 3.1 Add or update frontend unit tests for populated, empty, failure/retry, locale, and navigation behavior.
- [x] 3.2 Add a Project Overview E2E scenario covering dashboard rendering and navigation to an Issue.
- [x] 3.3 Run strict OpenSpec validation, backend tests, frontend unit tests, and production builds.
- [x] 3.4 Run the newly added or modified Project Overview E2E spec locally at least once.
