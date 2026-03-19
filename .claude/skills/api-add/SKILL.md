---
name: api-add
description: Add a new API endpoint with DTO, service, OpenAPI metadata, and EF Core database query
argument-hint: "[endpoint description, e.g. 'get all orgs' or 'create an event']"
---

Implement the following API endpoint: $ARGUMENTS

Follow these steps in order:

## 1. Consult the design docs

Read the relevant sections of these docs to understand the intended endpoint shape, request/response format, and database schema:
- `docs/api-design.md` — endpoint routes, HTTP methods, request/response contracts
- `docs/database-design.md` — entity relationships and column definitions

## 2. Create or update the Response DTO

- Location: `src/api/TimbnTicketing.Api/Dtos/Responses/`
- Use a `record` type
- Only include fields that should be publicly exposed (no internal IDs like foreign keys unless needed for client use)
- Naming: `{Resource}Response.cs`

## 3. Create a Request DTO (if the endpoint accepts a body)

- Location: `src/api/TimbnTicketing.Api/Dtos/Requests/`
- Use a `record` type
- Naming: `{Action}{Resource}Request.cs` (e.g. `CreateEventRequest.cs`)

## 4. Create or update the Service

- Location: `src/api/TimbnTicketing.Api/Services/{Resource}Service.cs`
- Inject `PlatformDbContext` via primary constructor
- Use `.Select()` projection to map directly into the Response DTO at the database level for read queries (this avoids materializing full entities and skips change tracking automatically)
- Register the service as scoped in `Program.cs` if it's new: `builder.Services.AddScoped<{Resource}Service>();`

## 5. Implement the endpoint handler

- Location: `src/api/TimbnTicketing.Api/Endpoints/{Resource}Endpoints.cs`
- Replace the `NotImplementedException` stub with the real implementation
- Inject the service (not DbContext) into the handler
- Keep the handler thin — just HTTP concerns (parameter binding, calling the service, returning status codes)

## 6. Add OpenAPI metadata

On the endpoint's `Map{Method}` call, chain:
- `.WithName("{ActionResource}")` — operation ID
- `.WithSummary("...")` — short description
- `.Produces<{ResponseDto}>()` — for success responses
- `.ProducesProblem(StatusCodes.Status404NotFound)` — for error responses as applicable
- The route group should already have `.WithTags("{Resource}")` — add it if missing

## 7. Verify

Run `dotnet build` from `src/api/` to confirm the solution compiles with zero errors and zero warnings.

## Conventions

- All monetary values are integer cents (2500 = $25.00)
- All database queries for org-scoped resources must filter by OrganizationId
- API routes use kebab-case and plural nouns
- JSON serialization is camelCase (automatic)
- C# uses PascalCase for properties
