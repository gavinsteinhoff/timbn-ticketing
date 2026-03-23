# Project Structure — timbn-ticketing

## Overview

Monorepo containing the API (C# / .NET 10 Minimal APIs) and the web dashboard (React / TypeScript / Vite), orchestrated locally with Aspire. Licensed under AGPLv3.

Repository: `github.com/timbn/timbn-ticketing`

---

## Repository Layout

```
timbn-ticketing/
├── README.md
├── LICENSE                                     # AGPLv3
├── .gitignore
├── .editorconfig
├── docs/
│   ├── database-design.md
│   ├── api-design.md
│   └── project-structure.md                    # This file
├── src/
│   ├── api/
│   │   ├── TimbnTicketing.sln
│   │   ├── TimbnTicketing.AppHost/             # Aspire orchestrator
│   │   ├── TimbnTicketing.ServiceDefaults/     # Shared Aspire defaults (health, telemetry, resilience)
│   │   ├── TimbnTicketing.Api/
│   │   ├── TimbnTicketing.Core/
│   │   ├── TimbnTicketing.Infrastructure/
│   │   └── TimbnTicketing.Tests/
│   └── web/
│       ├── package.json
│       ├── tsconfig.json
│       ├── vite.config.ts
│       └── src/
└── scripts/
    ├── seed-dev-data.sql                       # Sample org, users, events for local dev
    └── migrate.sh                              # Run EF Core migrations
```

---

## API — C# / .NET 10

### Solution Structure

All .NET projects live under `src/api/`. The solution file is at `src/api/TimbnTicketing.sln`.

```
src/api/
├── TimbnTicketing.sln
│
├── TimbnTicketing.AppHost/                     # Aspire orchestrator — starts everything
│   ├── TimbnTicketing.AppHost.csproj
│   └── Program.cs                              # App model: API, database, React dev server
│
├── TimbnTicketing.ServiceDefaults/             # Shared Aspire service defaults
│   ├── TimbnTicketing.ServiceDefaults.csproj
│   └── Extensions.cs                           # Health checks, OpenTelemetry, resilience
│
├── TimbnTicketing.Api/                         # Entry point, HTTP layer
│   ├── TimbnTicketing.Api.csproj
│   ├── Program.cs                              # App startup, DI, route group registration
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Endpoints/                              # One file per resource
│   │   ├── AuthEndpoints.cs
│   │   ├── OrganizationEndpoints.cs
│   │   ├── RoleEndpoints.cs
│   │   ├── MemberEndpoints.cs
│   │   ├── VenueEndpoints.cs
│   │   ├── EventEndpoints.cs
│   │   ├── TicketTypeEndpoints.cs
│   │   ├── EventTicketEndpoints.cs
│   │   ├── OrderEndpoints.cs
│   │   ├── CheckinEndpoints.cs
│   │   ├── DiscountCodeEndpoints.cs
│   │   ├── MetadataEndpoints.cs
│   │   ├── TicketClaimEndpoints.cs
│   │   ├── TicketEndpoints.cs
│   │   └── CurrentUserEndpoints.cs
│   ├── Auth/
│   │   ├── CurrentUserContext.cs               # Scoped service: user ID, org, bitwise permissions
│   │   ├── OrgResolutionMiddleware.cs          # Resolve orgSlug → OrganizationId
│   │   ├── MembershipResolutionMiddleware.cs   # Resolve user + org → role + permissions
│   │   ├── UserResolverMiddleware.cs           # Resolve JWT sub → UserId
│   │   └── PermissionEndpointFilter.cs         # Check role permission bits via HasFlag
│   ├── Extensions/
│   │   ├── HttpContextExtensions.cs            # GetOrgRole(), GetCurrentUser()
│   │   └── RouteBuilderExtensions.cs           # RequirePermission()
│   └── Dtos/
│       ├── Requests/                           # Incoming request shapes
│       │   ├── CheckoutRequest.cs
│       │   ├── CreateEventRequest.cs
│       │   └── ...
│       └── Responses/                          # Outgoing response shapes
│           ├── EventResponse.cs
│           ├── CheckinResponse.cs
│           └── ...
│
├── TimbnTicketing.Core/                        # Domain layer — no dependencies on infrastructure
│   ├── TimbnTicketing.Core.csproj
│   ├── Entities/                               # Domain models
│   │   ├── Organization.cs
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── UserOrganization.cs
│   │   ├── Venue.cs
│   │   ├── Event.cs
│   │   ├── TicketType.cs
│   │   ├── EventTicket.cs
│   │   ├── EventTicketDependency.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── UserTicket.cs
│   │   ├── DiscountCode.cs
│   │   ├── UserOrganizationMetadataInfo.cs
│   │   └── UserOrganizationMetadataValue.cs
│   ├── Interfaces/                             # Service and repository contracts
│   │   ├── IStripeConnectService.cs
│   │   ├── IStripeCheckoutService.cs
│   │   ├── ITicketValidationService.cs
│   │   ├── ITicketClaimService.cs
│   │   ├── IEmailService.cs
│   │   └── IQrCodeService.cs
│   └── Exceptions/                             # Domain-specific exceptions
│       ├── TicketSoldOutException.cs
│       ├── DependencyNotMetException.cs
│       ├── CheckinWindowClosedException.cs
│       └── ClaimExpiredException.cs
│
├── TimbnTicketing.Infrastructure/              # Data access, external services
│   ├── TimbnTicketing.Infrastructure.csproj
│   ├── Data/
│   │   ├── PlatformDbContext.cs                # EF Core DbContext
│   │   ├── Configurations/                     # Fluent API entity configurations
│   │   │   ├── OrganizationConfiguration.cs
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── EventTicketConfiguration.cs
│   │   │   └── ...
│   │   └── Migrations/                         # EF Core migrations
│   ├── Services/
│   │   ├── StripeConnectService.cs
│   │   ├── StripeCheckoutService.cs
│   │   ├── StripeWebhookService.cs
│   │   ├── TicketValidationService.cs
│   │   ├── TicketClaimService.cs
│   │   ├── QrCodeService.cs
│   │   └── EmailService.cs
│   └── Repositories/
│       ├── OrganizationRepository.cs
│       ├── EventRepository.cs
│       ├── OrderRepository.cs
│       └── ...
│
└── TimbnTicketing.Tests/
    ├── TimbnTicketing.Tests.csproj
    ├── Unit/
    │   ├── Services/
    │   │   ├── TicketValidationServiceTests.cs
    │   │   ├── TicketClaimServiceTests.cs
    │   │   └── ...
    │   └── Endpoints/
    │       └── ...
    ├── Integration/
    │   ├── CheckoutFlowTests.cs
    │   ├── CheckinFlowTests.cs
    │   └── ...
    └── Fixtures/
        ├── TestDbFixture.cs                    # Shared test database setup
        └── SeedData.cs
```

### Project Dependencies

```
TimbnTicketing.AppHost
  ├── references → TimbnTicketing.Api (orchestrates it)
  └── references → TimbnTicketing.ServiceDefaults

TimbnTicketing.Api
  ├── references → TimbnTicketing.Core
  ├── references → TimbnTicketing.Infrastructure
  └── references → TimbnTicketing.ServiceDefaults

TimbnTicketing.Infrastructure
  └── references → TimbnTicketing.Core

TimbnTicketing.Core
  └── references → nothing (no outward dependencies)

TimbnTicketing.ServiceDefaults
  └── references → nothing (shared config only)

TimbnTicketing.Tests
  ├── references → TimbnTicketing.Api
  ├── references → TimbnTicketing.Core
  └── references → TimbnTicketing.Infrastructure
```

The Core project has zero dependencies on Infrastructure or Api. All external concerns (database, Stripe, email) are defined as interfaces in Core and implemented in Infrastructure. This keeps domain logic testable without needing real services.

The AppHost project references the Api project so Aspire can discover and launch it, but it contains no business logic — just the orchestration model. The ServiceDefaults project is shared config that gets added to the Api (and any future services) to provide consistent health checks, OpenTelemetry, and HTTP resilience.

### Key NuGet Packages

| Package | Project | Purpose |
|---------|---------|---------|
| `Aspire.Hosting` | AppHost | Aspire orchestration |
| `Aspire.Hosting.SqlServer` | AppHost | SQL Server resource support |
| `Aspire.Hosting.NodeJs` | AppHost | React dev server orchestration |
| `Aspire.Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | Aspire-aware EF Core SQL Server integration |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Api | Firebase JWT validation |
| `Microsoft.EntityFrameworkCore` | Infrastructure | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | SQL Server provider |
| `Microsoft.Extensions.Http.Resilience` | ServiceDefaults | Retry, circuit breaker, timeout policies |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | ServiceDefaults | Telemetry export to Aspire dashboard |
| `Stripe.net` | Infrastructure | Stripe Connect / Checkout |
| `QRCoder` | Infrastructure | QR code image generation |
| `FluentValidation` | Api | Request validation |
| `xunit` | Tests | Test framework |
| `Aspire.Hosting.Testing` | Tests | Integration testing with Aspire |

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| Entities | PascalCase, singular | `EventTicket`, `UserOrganization` |
| DB tables | PascalCase, plural | `EventTickets`, `UserOrganizations` |
| DB columns | PascalCase | `CheckedInAt`, `StripeConnectAccountId` |
| API routes | kebab-case, plural | `/orgs/{orgSlug}/ticket-types` |
| JSON fields | camelCase | `eventTicketId`, `checkedInAt` |
| C# properties | PascalCase | `EventTicketId`, `CheckedInAt` |
| Endpoint files | PascalCase + "Endpoints" | `OrderEndpoints.cs` |
| Service files | PascalCase + "Service" | `TicketClaimService.cs` |

EF Core maps PascalCase C# properties to PascalCase SQL Server columns by default — no special configuration needed for the database layer. The API serializes to camelCase JSON (the .NET default).

### JSON Serialization

.NET defaults to camelCase for JSON serialization, so C# DTOs using PascalCase (`CheckedInAt`) automatically serialize to camelCase (`checkedInAt`) with no extra configuration. If you need to customize behavior (e.g., enum handling), configure it in `Program.cs`:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

---

## Web Dashboard — React / TypeScript / Vite

### Stack

| Tool | Purpose |
|------|---------|
| React 19 | UI framework |
| TypeScript | Type safety |
| Vite | Dev server and bundler |
| React Router | Client-side routing |
| TanStack Query | Server state management, caching, mutations |
| shadcn/ui | Component library (copy-paste, not a dependency) |
| Tailwind CSS | Utility-first styling |
| Firebase SDK | Authentication |

### Frontend Structure

```
src/web/
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.ts
├── index.html
├── public/
│   └── favicon.svg
└── src/
    ├── main.tsx                                # App entry, providers
    ├── App.tsx                                 # Root layout, route definitions
    ├── api/                                    # API client layer
    │   ├── client.ts                           # Fetch wrapper with auth headers
    │   ├── types.ts                            # Shared API response/request types
    │   ├── events.ts                           # Event-related API calls
    │   ├── orders.ts                           # Order/checkout API calls
    │   ├── members.ts                          # Member search, metadata
    │   └── ...
    ├── hooks/                                  # Custom React hooks
    │   ├── useAuth.ts                          # Firebase auth wrapper
    │   ├── useCurrentOrg.ts                    # Current org context
    │   └── usePermission.ts                    # Check role permissions
    ├── components/                             # Reusable UI components
    │   ├── ui/                                 # shadcn/ui components
    │   ├── layout/
    │   │   ├── Sidebar.tsx
    │   │   ├── Header.tsx
    │   │   └── OrgSwitcher.tsx
    │   ├── tickets/
    │   │   ├── TicketSelector.tsx
    │   │   ├── AttendeeSearch.tsx              # Member search for gift purchases
    │   │   └── QrScanner.tsx                   # Camera-based QR reader for check-in
    │   └── events/
    │       ├── EventCard.tsx
    │       └── EventForm.tsx
    ├── pages/                                  # Route-level page components
    │   ├── public/                             # No auth required
    │   │   ├── EventListPage.tsx
    │   │   ├── EventDetailPage.tsx
    │   │   ├── CheckoutPage.tsx
    │   │   ├── OrderConfirmationPage.tsx
    │   │   └── ClaimTicketPage.tsx
    │   ├── dashboard/                          # Auth required, org-scoped
    │   │   ├── DashboardHomePage.tsx
    │   │   ├── EventManagementPage.tsx
    │   │   ├── OrderListPage.tsx
    │   │   ├── CheckinPage.tsx                 # QR scanner + stats
    │   │   ├── MemberListPage.tsx
    │   │   ├── DiscountCodePage.tsx
    │   │   ├── RoleManagementPage.tsx
    │   │   └── OrgSettingsPage.tsx
    │   └── account/                            # Auth required, user-scoped
    │       ├── MyTicketsPage.tsx
    │       ├── GiftedTicketsPage.tsx
    │       ├── OrderHistoryPage.tsx
    │       └── ProfilePage.tsx
    ├── contexts/
    │   └── OrgContext.tsx                       # Current org + role in React context
    ├── lib/
    │   └── utils.ts                            # Formatting helpers (cents → dollars, dates, etc.)
    └── types/
        └── index.ts                            # Shared frontend types
```

### URL Routes

The frontend routes mirror the API's org-scoped structure:

| Route | Page | Auth |
|-------|------|------|
| `/:orgSlug` | Event list (public) | No |
| `/:orgSlug/events/:eventSlug` | Event detail + ticket selection | No |
| `/:orgSlug/events/:eventSlug/checkout` | Checkout flow | Yes |
| `/:orgSlug/events/:eventSlug/confirmation/:orderId` | Order confirmation | Yes |
| `/claim/:claimToken` | Ticket claim landing page | No (prompts login) |
| `/my/tickets` | My tickets across all orgs | Yes |
| `/my/gifts` | Gifted tickets I purchased | Yes |
| `/my/orders` | Order history | Yes |
| `/my/profile` | Profile settings | Yes |
| `/:orgSlug/dashboard` | Admin dashboard home | Yes + role |
| `/:orgSlug/dashboard/events` | Event management | Yes + `CanManageEvents` |
| `/:orgSlug/dashboard/events/:eventSlug` | Edit event | Yes + `CanManageEvents` |
| `/:orgSlug/dashboard/events/:eventSlug/orders` | Event orders | Yes + `CanViewAttendees` |
| `/:orgSlug/dashboard/events/:eventSlug/checkin` | QR check-in scanner | Yes + `CanCheckin` |
| `/:orgSlug/dashboard/members` | Member list | Yes + `CanViewAttendees` |
| `/:orgSlug/dashboard/discount-codes` | Discount codes | Yes + `CanManageEvents` |
| `/:orgSlug/dashboard/roles` | Role management | Yes + `CanManageRoles` |
| `/:orgSlug/dashboard/settings` | Org settings, Stripe Connect | Yes + `CanManageOrganization` |

### API Client Pattern

The `src/api/client.ts` file wraps `fetch` with auth token injection, base URL, and error handling:

```typescript
// src/api/client.ts
import { getAuth } from 'firebase/auth';

const BASE_URL = import.meta.env.VITE_API_URL; // tickets.timbn.com/api/v1

export async function apiClient<T>(
  path: string,
  options: RequestInit = {},
  token?: string
): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...(token && { Authorization: `Bearer ${token}` }),
    ...options.headers,
  };

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await response.json();
    throw new ApiError(response.status, error);
  }

  return response.json();
}
```

Used with TanStack Query:

```typescript
// src/api/events.ts
export function useEvents(orgSlug: string) {
  const auth = getAuth();

  return useQuery({
    queryKey: ['events', orgSlug],
    queryFn: async () => {
      const token = await getAccessTokenSilently();
      return apiClient<EventListResponse>(
        `/orgs/${orgSlug}/events`,
        {},
        token
      );
    },
  });
}
```

### Permission-Gated UI

Dashboard pages check the user's role permissions via `OrgContext`. The `usePermission` hook wraps this:

```typescript
// src/hooks/usePermission.ts
export function usePermission(permission: number): boolean {
  const { permissions } = useCurrentOrg();
  return (permissions & permission) === permission;
}

// Usage in a component
function EventManagementPage() {
  const canManage = usePermission(Permission.CanManageEvents);
  if (!canManage) return <Forbidden />;
  // ...
}
```

Routes in `App.tsx` use a `<ProtectedRoute>` wrapper that checks auth and optionally a permission before rendering the page.

---

## Local Development

### Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm
- SQL Server LocalDB (ships with Visual Studio and the .NET SDK)

### Getting Started

```bash
# Clone the repo
git clone https://github.com/timbn/timbn-ticketing.git
cd timbn-ticketing

# Install web dependencies
cd src/web
npm install
cd ..

# Run everything via Aspire
cd api
dotnet run --project TimbnTicketing.AppHost
```

That's it. The Aspire AppHost starts the API, the React dev server, and connects to LocalDB — all from a single command. The Aspire dashboard opens automatically in your browser at `https://localhost:17225`, showing logs, traces, and resource status for everything.

### Aspire AppHost

The AppHost defines the entire local application model:

```csharp
// src/api/TimbnTicketing.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("ticketing");

var api = builder.AddProject<Projects.TimbnTicketing_Api>("api")
    .WithReference(sql);

builder.AddNpmApp("web", "../../web", "dev")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

This connects to your existing LocalDB instance via a connection string in `appsettings.Development.json`. If you later want Aspire to spin up a SQL Server container instead, swap `AddConnectionString` for `AddSqlServer("sql").AddDatabase("ticketing")` — but that requires Docker.

### Aspire ServiceDefaults

The ServiceDefaults project provides shared configuration that gets added to the API (and any future services). It wires up:

- **OpenTelemetry** — structured logging, distributed tracing, and metrics exported to the Aspire dashboard
- **Health checks** — `/health` and `/alive` endpoints for readiness and liveness
- **HTTP resilience** — automatic retries, circuit breakers, and timeouts on outgoing HTTP calls
- **Service discovery** — resolves named references between services (e.g., the React app can reference the API by name)

The API opts into these defaults in its `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ... register services, configure auth, etc.

var app = builder.Build();
app.MapDefaultEndpoints(); // maps /health and /alive
```

### Environment Variables

**API (`src/api/TimbnTicketing.Api/appsettings.Development.json`):**

```json
{
  "ConnectionStrings": {
    "ticketing": "Server=(localdb)\\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=true;TrustServerCertificate=true"
  },
  "FirebaseProjectId": "timbn-ticketing",
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PlatformFeePercent": 3
  }
}
```

**Web (`src/web/.env.local`):**

```
VITE_API_URL=http://localhost:5000/api/v1
VITE_FIREBASE_API_KEY=your-firebase-api-key
VITE_FIREBASE_AUTH_DOMAIN=timbn-ticketing.firebaseapp.com
VITE_FIREBASE_PROJECT_ID=timbn-ticketing
```

Note: When running through Aspire, the API URL may be assigned dynamically. The `WithReference(api)` in the AppHost passes the API's endpoint to the web app as an environment variable. You can configure the React app to read from `services__api__https__0` (Aspire's service discovery format) or keep the explicit `VITE_API_URL` for simplicity during early development.

### Azure Hosting Plan

| Service | Azure Resource | Tier |
|---------|---------------|------|
| API | Azure App Service | B1 to start, scale as needed |
| Web dashboard | Azure Static Web Apps | Free tier (generous for SPAs) |
| Database | Azure SQL Database | Serverless (scales to zero, pay per use) |
| Auth | Firebase Authentication | Free tier (unlimited email/password, 10k/month for phone) |
| Payments | Stripe Connect | Pay as you go |
| DNS | Azure DNS or Cloudflare | `tickets.timbn.com` |

**Azure SQL Serverless** is ideal for bootstrapping — it auto-pauses when inactive and you pay only for compute seconds used. There's a ~60 second cold start on first query after a pause, which is fine for a product that isn't seeing constant traffic yet.

**Azure Static Web Apps** serves the built React app from a CDN for free, handles HTTPS, and can be configured to proxy `/api/*` requests to your App Service so everything runs under `tickets.timbn.com` with no CORS issues.

---

## CI / CD

Not configured yet. Planned pipeline:

1. **On PR:** lint, build, run unit tests, run integration tests (using `Aspire.Hosting.Testing` for full-stack test scenarios)
2. **On merge to main:** deploy API to Azure App Service staging slot, deploy web to Azure Static Web Apps preview
3. **On release tag:** swap staging slot to production, promote Static Web Apps preview to production

GitHub Actions is the likely choice given the repo is on GitHub. Azure has first-party GitHub Actions for App Service and Static Web Apps deployment.

---

## Future Additions

These are not yet implemented but have a natural home in the structure:

- **Tournament system** — `TimbnTicketing.Core/Entities/Tournament.cs`, `TournamentEndpoints.cs`, tournament-related pages in `src/web/src/pages/dashboard/`
- **Email templates** — `src/api/TimbnTicketing.Infrastructure/Templates/` with Razor or Liquid templates for confirmation, claim, and reminder emails
- **File uploads** — presigned Azure Blob Storage URLs generated by a `FileUploadService`, used for event banners, org logos, venue maps
- **Webhook event log** — table and admin page to replay or inspect failed Stripe webhooks
- **Public API docs** — Swagger/OpenAPI generated from the Minimal API endpoints, hosted at `tickets.timbn.com/api/docs`
- **Aspire deployment** — Aspire supports `aspire do` for automated Azure deployment. When ready, add an Azure deployment manifest to the AppHost to provision Azure SQL, App Service, and Static Web Apps directly from the app model
- **Background workers** — claim expiration, email sending, Stripe webhook retries. Add as a separate `TimbnTicketing.Worker` project referenced by the AppHost, with shared ServiceDefaults for consistent telemetry
- **Redis caching** — add `builder.AddRedis("cache")` to the AppHost and reference it from the API for session caching and rate limiting
