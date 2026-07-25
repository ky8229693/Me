## ADDED Requirements

### Requirement: CI runs for proposed and integrated changes
The repository SHALL run its CI pipeline for pull requests targeting `master` and pushes to `master`. A newer run for the same pull request SHALL cancel an obsolete in-progress run.

#### Scenario: Contributor updates a pull request
- **WHEN** a contributor pushes a new commit to an open pull request targeting `master`
- **THEN** CI starts for the new commit and cancels an older in-progress run for that pull request

#### Scenario: Change is merged
- **WHEN** a commit is pushed to `master`
- **THEN** CI verifies the exact integrated commit

### Requirement: CI validates backend and frontend builds and tests
The CI pipeline SHALL restore locked dependencies, build the ASP.NET Core API in Release configuration, run all backend tests, install frontend dependencies with `npm ci`, run frontend unit tests, and produce the frontend production build.

#### Scenario: Backend test fails
- **WHEN** any backend test returns a failing exit code
- **THEN** the backend CI job fails and reports the failed test

#### Scenario: Frontend type or production build fails
- **WHEN** Vue TypeScript checking or Vite production build fails
- **THEN** the frontend CI job fails

### Requirement: CI validates OpenSpec artifacts
The CI pipeline SHALL validate all main specs and active changes using the repository OpenSpec command.

#### Scenario: Delta spec structure is invalid
- **WHEN** an active change contains an invalid requirement or scenario structure
- **THEN** the OpenSpec CI job fails before the change can be treated as passing

### Requirement: CI exercises browser workflows against an isolated database
The CI pipeline SHALL run the existing Playwright E2E suite against an ephemeral PostgreSQL service and SHALL NOT connect to a development or production database.

#### Scenario: E2E job starts
- **WHEN** CI prepares the Playwright E2E job
- **THEN** it supplies an isolated PostgreSQL maintenance connection and installs the required Chromium runtime

#### Scenario: Browser test fails
- **WHEN** a Playwright test fails
- **THEN** the E2E job fails and preserves available diagnostic artifacts for a limited retention period

### Requirement: CI verifies deployable container definitions
The CI pipeline SHALL render the Compose configuration with non-secret CI placeholder values and SHALL build the API and Client container images without publishing or deploying them.

#### Scenario: Compose interpolation is invalid
- **WHEN** a required Compose variable or service definition cannot be resolved
- **THEN** the container CI job fails during configuration validation

#### Scenario: Container build succeeds
- **WHEN** both production Dockerfiles build successfully
- **THEN** CI records the container job as passing without pushing images to a registry

### Requirement: CI follows a minimal trust model
The workflow SHALL use read-only repository contents permission, SHALL NOT use production secrets, SHALL NOT use `pull_request_target`, and SHALL constrain each job with a finite timeout.

#### Scenario: Pull request code runs
- **WHEN** CI executes code supplied by a pull request
- **THEN** the job has no deployment credentials or write-capable repository token

#### Scenario: Job hangs
- **WHEN** a CI command exceeds its configured job timeout
- **THEN** GitHub Actions terminates the job and reports a failure

### Requirement: CI usage is documented
The repository SHALL document each CI job, its local equivalent commands, expected environment dependencies, failure artifacts, and the explicit exclusion of continuous deployment.

#### Scenario: Developer investigates a failure
- **WHEN** a CI job fails
- **THEN** the developer can identify the matching local command and relevant diagnostic output from repository documentation
