## ADDED Requirements

### Requirement: Local production environment starts reproducibly
The repository SHALL provide a Docker Compose configuration that builds and starts the Vue client, ASP.NET Core API, PostgreSQL database, and HTTPS gateway using production builds and non-Development runtime settings.

#### Scenario: Operator starts the environment
- **WHEN** an operator supplies the documented local secrets and runs the documented Compose startup command
- **THEN** all required services start and become healthy without using the Vite development server or `dotnet run`

#### Scenario: Operator uses the normal development workflow
- **WHEN** an operator starts the existing API and Vite development commands without Compose
- **THEN** the existing HTTP development URLs and proxy behavior remain available

### Requirement: Local gateway provides a same-origin HTTPS application
The local deployment SHALL expose the client at `https://localhost`, SHALL proxy `/api` requests to the API service, and SHALL use a locally supplied TLS certificate and private key that are not committed to source control.

#### Scenario: User signs in through the local gateway
- **WHEN** a user signs in at `https://localhost`
- **THEN** API requests remain same-origin and the refresh cookie is issued with the Production `Secure` policy

#### Scenario: Certificate files are absent
- **WHEN** the operator starts the gateway without the documented certificate and private-key files
- **THEN** the gateway fails visibly instead of silently serving an insecure Production endpoint

### Requirement: Routed frontend pages survive direct requests
The gateway SHALL serve the Vue `index.html` for unmatched non-API application routes and SHALL NOT apply the SPA fallback to `/api` requests.

#### Scenario: User refreshes an Issue route
- **WHEN** a user directly requests or refreshes `/projects/{projectId}/issues/{issueId}`
- **THEN** the gateway returns the SPA entry point and Vue Router resolves the page

#### Scenario: API route is unknown
- **WHEN** a client requests an unknown route under `/api`
- **THEN** the request is handled by the API service and is not replaced with the Vue entry point

### Requirement: Database data persists and services expose health
The local deployment SHALL store PostgreSQL data in a named volume, SHALL wait for PostgreSQL health before starting the API, and SHALL expose enough container health information to diagnose startup.

#### Scenario: Environment is restarted
- **WHEN** the operator stops and restarts the Compose services without deleting volumes
- **THEN** previously committed application data remains available

#### Scenario: Database is unavailable
- **WHEN** PostgreSQL does not pass its health check
- **THEN** the API is not treated as ready and dependent gateway startup does not report a healthy application

### Requirement: Local secrets stay outside version control
The repository SHALL provide a non-secret environment template and SHALL ignore the populated environment file, generated certificate private keys, and local database backups.

#### Scenario: Operator prepares configuration
- **WHEN** an operator copies the environment template and supplies a database password and JWT signing key
- **THEN** Compose injects the values without requiring secrets in committed application settings

#### Scenario: Operator checks repository changes
- **WHEN** local deployment secrets, certificates, and backups have been generated in their documented locations
- **THEN** Git does not list those sensitive local artifacts as files to commit

### Requirement: Operators can practice backup restore and lifecycle operations
The repository SHALL document and provide commands or scripts for build, startup, health verification, Admin bootstrap, backup, restore, update, logs, shutdown, and explicit data removal.

#### Scenario: Operator backs up and restores the exercise database
- **WHEN** an operator follows the documented backup and restore procedure with a selected backup file
- **THEN** the PostgreSQL database can be restored and verified through the application

#### Scenario: Operator performs a normal shutdown
- **WHEN** an operator runs the documented non-destructive shutdown command
- **THEN** containers stop while the PostgreSQL named volume remains intact
