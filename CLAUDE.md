# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Multi-tenant event ticketing platform (AGPLv3). Backend is .NET 10 Minimal APIs with SQL Server, orchestrated locally with .NET Aspire. A React 19 + TypeScript + Vite frontend is planned but not yet created.

**Current state:** Core project structure is in place with all EF Core entities and configurations. API endpoints are stubbed with `NotImplementedException`. Tests and web frontend are not yet created.

## Build & Run Commands

```bash
# Run the full stack via Aspire
aspire start

# Build all projects
cd src/api
dotnet build

# Run tests (once Tests project exists)
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# EF Core migrations (connection string must be passed since it lives in AppHost config)
cd src/api/TimbnTicketing.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --startup-project ../TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"
```

Health endpoints: `/health` (readiness), `/alive` (liveness)

## Architecture

### Solution Layout (under `src/api/`)

- **AppHost** — Aspire orchestrator. Defines the app model (API + database connection + Stripe secret parameter). No business logic.
- **ServiceDefaults** — Shared config added to all services: OpenTelemetry, health checks, HTTP resilience (retry/circuit breaker/timeout), service discovery.
- **Api** — HTTP entry point. Minimal API endpoints in `Endpoints/` (one file per resource), middleware for org/membership resolution, permission endpoint filters via `RequirePermission()`, DTOs in `Dtos/Requests/` and `Dtos/Responses/`.
- **Core** — Domain layer with zero external dependencies. Entities, service interfaces, domain exceptions.
- **Infrastructure** — EF Core DbContext (`PlatformDbContext`), entity configurations (Fluent API), dev data seeding (`DevelopmentDataSeeder` via `UseSeeding`/`UseAsyncSeeding`), service implementations (Stripe, QR codes, email), repositories.
- **Tools.Migration** — Console app for migrating KCGameOn users from MySQL to Timbn. Two commands: `export` (MySQL → JSON) and `import` (JSON → SQL Server via PlatformDbContext).
- **Tests** — xunit. Unit tests for services/endpoints, integration tests using `Aspire.Hosting.Testing`.

### Dependency Flow

```text
AppHost → Api, ServiceDefaults
Api → Core, Infrastructure, ServiceDefaults
Infrastructure → Core
Tools.Migration → Infrastructure, Core
Core → nothing
```

Core defines interfaces; Infrastructure implements them. This keeps domain logic free of external concerns.

### Multi-Tenancy Pattern

All API routes are scoped by `{orgSlug}`. Two middleware components resolve context:

1. `OrgResolutionMiddleware` — resolves slug → OrganizationId
2. `MembershipResolutionMiddleware` — resolves user + org → role

All database queries must be scoped to OrganizationId to prevent cross-tenant data leakage.

### Auth & Permissions

- Firebase JWTs for authentication
- Bitwise permissions (Discord-style): Role entity stores a single `Permission Permissions` field (backed by `bigint`). The `Permission` enum is `[Flags] : long` with power-of-2 values (`1L << 0` through `1L << 6`). New permissions are added by appending the next bit shift — positions must never be reused or reordered.
- Role hierarchy: lower number = higher privilege. Users can only manage roles with higher hierarchy numbers than their own.
- `CurrentRequestContext` holds a `Permission` flags field; `HasPermission()` uses `HasFlag()` (single AND instruction)
- Endpoints use `RequirePermission("CanXxx")` extension which triggers `PermissionEndpointFilter`

### Key Domain Concepts

- **Orders vs UserTickets**: Orders track the purchaser; UserTickets track the attendee. Separated to support gifting tickets.
- **Ticket dependencies**: EventTicketDependencies use AND/OR logic for prerequisites (e.g., food add-on requires GA OR VIP).
- **Claim flow**: Gifted tickets start as `pendingClaim` until the recipient claims via a secure token.
- **Custom profile fields**: EAV pattern via UserOrganizationMetadataInfo/Values — orgs define custom fields without schema changes.
- **Money**: All monetary values stored as integer cents (2500 = $25.00).
- **Checkout concurrency**: Ticket capacity and discount code usage are protected by serializable transactions with pessimistic locking (`UPDLOCK, HOLDLOCK`) to prevent overselling.
- **User migration**: KCGameOn users are migrated with `AuthProviderId = "kcgo-migrated-{id}"`. `UserResolverMiddleware` links them to a real Firebase UID on first sign-in by matching email (only for accounts with the `kcgo-migrated-` prefix).

## Naming Conventions

| Item | Convention | Example |
| --- | --- | --- |
| C# properties | PascalCase | `EventTicketId` |
| DB tables | PascalCase, plural | `EventTickets` |
| DB columns | PascalCase | `CheckedInAt` |
| API routes | kebab-case, plural | `/orgs/{orgSlug}/ticket-types` |
| JSON fields | camelCase (auto) | `checkedInAt` |
| Endpoint files | `{Resource}Endpoints.cs` | `OrderEndpoints.cs` |
| Service files | `{Service}Service.cs` | `TicketClaimService.cs` |

## Endpoint Conventions

When implementing or modifying Minimal API endpoints, **always** include full OpenAPI metadata:

- `.WithName("OperationName")` — unique operation ID
- `.WithSummary("Short description")` — one-line summary
- `.WithDescription("...")` — detailed description when non-obvious
- `.Accepts<TRequest>("application/json")` — for endpoints with request bodies
- `.Produces<TResponse>(statusCode)` — for each success response
- `.ProducesProblem(statusCode)` — for each error response

## Design Documentation

Comprehensive design docs exist in `/docs/`:

- `project-structure.md` — Full planned project layout, dependency graph, frontend structure, deployment plan
- `api-design.md` — REST API patterns, all endpoints, auth, pagination, error handling
- `database-design.md` — Schema definitions, relationships, design decisions

**Consult these docs before implementing new features** — they describe the intended architecture in detail.
