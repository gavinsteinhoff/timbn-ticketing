# API Design — Multi-Tenant Ticketing Platform

## Overview

RESTful API built with .NET 10 Minimal APIs. All endpoints return JSON. Authentication is handled via Firebase JWTs. Multi-tenancy is enforced by scoping all queries to the organization identified in the route.

Base URL: `https://api.yourplatform.com`

---

## Conventions

### Authentication

Most endpoints require a valid Firebase JWT in the `Authorization: Bearer {token}` header. The JWT contains the user's `AuthProviderId` (sub claim), which is resolved to the platform `UserId` on each request. Public-facing endpoints (e.g., viewing public orgs and events) allow anonymous access via `.AllowAnonymous()`.

### Multi-Tenancy

Organization-scoped routes use `/orgs/{orgSlug}/...`. The API resolves `orgSlug` to `OrganizationId` and scopes all queries accordingly. A middleware component validates that the authenticated user has a `UserOrganizations` record for the resolved org and attaches their role to the request context.

### Authorization

Permissions use a bitwise flags pattern (Discord-style). The Role entity stores a single `Permission Permissions` field (backed by `bigint`) where each bit represents a capability. A reusable authorization filter reads the required permission from endpoint metadata and checks it against the request context using `HasFlag()`.

```csharp
// Example: require can_manage_events permission
app.MapPost("/orgs/{orgSlug}/events", HandleCreateEvent)
   .RequirePermission("CanCreateEvents");
```

### Pagination

List endpoints support cursor-based pagination via query parameters:

- `?limit=25` — items per page (default 25, max 100)
- `?cursor={opaqueCursor}` — cursor from previous response

Responses include:

```json
{
  "data": [...],
  "pagination": {
    "nextCursor": "abc123",
    "hasMore": true
  }
}
```

### Error Responses

All errors follow a consistent shape:

```json
{
  "error": {
    "code": "TICKET_SOLD_OUT",
    "message": "This ticket type is no longer available.",
    "details": {}
  }
}
```

Standard HTTP status codes: 400 (validation), 401 (unauthenticated), 403 (insufficient permissions), 404 (not found), 409 (conflict), 422 (business rule violation).

### Route Group Structure

```csharp
// Program.cs — route group registration
var app = builder.Build();

app.MapGroup("/auth")
   .MapAuthEndpoints();

app.MapGroup("/orgs")
   .MapOrganizationEndpoints();

app.MapGroup("/orgs/{orgSlug}/roles")
   .MapRoleEndpoints();

app.MapGroup("/orgs/{orgSlug}/members")
   .MapMemberEndpoints();

app.MapGroup("/orgs/{orgSlug}/venues")
   .MapVenueEndpoints();

app.MapGroup("/orgs/{orgSlug}/events")
   .MapEventEndpoints();

app.MapGroup("/orgs/{orgSlug}/ticket-types")
   .MapTicketTypeEndpoints();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/tickets")
   .MapEventTicketEndpoints();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/orders")
   .MapOrderEndpoints();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/checkin")
   .MapCheckinEndpoints();

app.MapGroup("/orgs/{orgSlug}/discount-codes")
   .MapDiscountCodeEndpoints();

app.MapGroup("/orgs/{orgSlug}/metadata")
   .MapMetadataEndpoints();

app.MapGroup("/tickets/claim")
   .MapTicketClaimEndpoints();

app.MapGroup("/tickets")
   .MapTicketEndpoints();

app.MapGroup("/me")
   .MapCurrentUserEndpoints();
```

---

## Endpoints

---

### Auth

Handles post-authentication callback logic. Firebase manages the actual login flow — these endpoints handle platform-side user provisioning and token exchange.

#### `POST /auth/callback`

Called after Firebase authentication. Creates or updates the platform User record from the Firebase profile. Returns a platform session or token enriched with org memberships.

**Auth:** Public (called with Firebase token)

**Request body:**

```json
{
  "firebaseToken": "eyJhbGciOi..."
}
```

**Response:** `200 OK`

```json
{
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "firstName": "Alex",
    "lastName": "Johnson",
    "organizations": [
      {
        "orgId": "uuid",
        "orgSlug": "kcgameon",
        "orgName": "KCGameOn",
        "role": {
          "id": "uuid",
          "slug": "admin",
          "name": "Admin"
        }
      }
    ]
  },
  "token": "platform_jwt_here"
}
```

---

### Current User

Endpoints for the authenticated user to manage their own profile and view their tickets across all organizations.

#### `GET /me`

Returns the authenticated user's profile and org memberships.

**Auth:** Authenticated

**Response:** `200 OK`

```json
{
  "id": "uuid",
  "email": "user@example.com",
  "firstName": "Alex",
  "lastName": "Johnson",
  "createdAt": "2026-01-15T00:00:00Z",
  "organizations": [
    {
      "orgId": "uuid",
      "orgSlug": "kcgameon",
      "orgName": "KCGameOn",
      "role": { "id": "uuid", "slug": "member", "name": "Member" },
      "metadata": {
        "username": "FragMaster99",
        "discord_handle": "frag#1234"
      }
    }
  ]
}
```

#### `PATCH /me`

Update the authenticated user's core profile.

**Auth:** Authenticated

**Request body:**

```json
{
  "firstName": "Alex",
  "lastName": "Johnson"
}
```

**Response:** `200 OK` — updated user object

#### `GET /me/tickets`

Returns all tickets for the authenticated user across all organizations.

**Auth:** Authenticated

**Query params:** `?status=valid,checkedIn,pendingClaim` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "ticketCode": "TK-abc123",
      "status": "valid",
      "event": {
        "id": "uuid",
        "name": "KCGameOn Summer 2026",
        "slug": "summer-2026",
        "startsAt": "2026-07-15T10:00:00Z",
        "venue": {
          "name": "Bartle Hall",
          "address": "301 W 13th St, Kansas City, MO"
        }
      },
      "organization": {
        "slug": "kcgameon",
        "name": "KCGameOn"
      },
      "ticketType": "General Admission",
      "qrCodeUrl": "https://api.yourplatform.com/tickets/TK-abc123/qr",
      "createdAt": "2026-06-01T12:00:00Z"
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false }
}
```

#### `GET /me/gifted-tickets`

Returns tickets the authenticated user has purchased for others, including pending claims. Useful for the buyer to track whether gifts have been claimed.

**Auth:** Authenticated

**Query params:** `?status=valid,pendingClaim` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "ticketCode": "TK-def456",
      "status": "pendingClaim",
      "claimEmail": "charlie@example.com",
      "claimExpiresAt": "2026-07-15T10:00:00Z",
      "event": {
        "name": "KCGameOn Summer 2026",
        "slug": "summer-2026",
        "startsAt": "2026-07-15T10:00:00Z"
      },
      "organization": { "slug": "kcgameon", "name": "KCGameOn" },
      "ticketType": "General Admission",
      "createdAt": "2026-06-01T12:00:00Z"
    },
    {
      "id": "uuid",
      "ticketCode": "TK-ghi789",
      "status": "valid",
      "attendee": {
        "firstName": "Bob",
        "lastName": "Smith",
        "metadata": { "username": "BobGames" }
      },
      "event": {
        "name": "KCGameOn Summer 2026",
        "slug": "summer-2026",
        "startsAt": "2026-07-15T10:00:00Z"
      },
      "organization": { "slug": "kcgameon", "name": "KCGameOn" },
      "ticketType": "General Admission",
      "createdAt": "2026-06-01T12:00:00Z"
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false }
}
```

#### `GET /me/orders`

Returns the authenticated user's order history across all organizations.

**Auth:** Authenticated

**Query params:** `?status=completed` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "status": "completed",
      "totalCents": 5000,
      "event": { "name": "KCGameOn Summer 2026", "slug": "summer-2026" },
      "organization": { "slug": "kcgameon", "name": "KCGameOn" },
      "items": [
        {
          "ticketType": "General Admission",
          "quantity": 2,
          "priceCents": 2500
        }
      ],
      "createdAt": "2026-06-01T12:00:00Z"
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false }
}
```

---

### Organizations

#### `POST /orgs`

Create a new organization. The authenticated user becomes the owner.

**Auth:** Authenticated

**Request body:**

```json
{
  "name": "KCGameOn",
  "slug": "kcgameon",
  "websiteUrl": "https://kcgameon.com"
}
```

**Response:** `201 Created`

```json
{
  "id": "uuid",
  "name": "KCGameOn",
  "slug": "kcgameon",
  "websiteUrl": "https://kcgameon.com",
  "createdAt": "2026-01-15T00:00:00Z"
}
```

**Side effects:** Creates default roles (Owner, Admin, Staff, Member) for the org. Assigns the creating user the Owner role.

#### `GET /orgs/{orgSlug}`

Get organization details. Public orgs are accessible without authentication. Private orgs return 404 for non-members (to avoid revealing the org exists).

**Auth:** Anonymous (public orgs). Authenticated members see additional fields.

**Response:** `200 OK`

```json
{
  "id": "uuid",
  "name": "KCGameOn",
  "slug": "kcgameon",
  "logoUrl": "https://...",
  "websiteUrl": "https://kcgameon.com"
}
```

#### `PATCH /orgs/{orgSlug}`

Update organization details.

**Auth:** `CanManageOrganization`

**Request body:**

```json
{
  "name": "KCGameOn",
  "logoUrl": "https://...",
  "websiteUrl": "https://kcgameon.com"
}
```

**Response:** `200 OK` — updated org object

#### `POST /orgs/{orgSlug}/stripe-connect`

Initiate Stripe Connect onboarding. Returns a Stripe Account Link URL that the org owner visits to connect their Stripe account.

**Auth:** `CanManageBilling`

**Response:** `200 OK`

```json
{
  "onboardingUrl": "https://connect.stripe.com/setup/..."
}
```

#### `GET /orgs/{orgSlug}/stripe-connect/status`

Check whether the org has completed Stripe Connect onboarding.

**Auth:** `CanManageBilling`

**Response:** `200 OK`

```json
{
  "connected": true,
  "chargesEnabled": true,
  "payoutsEnabled": true
}
```

---

### Roles

#### `GET /orgs/{orgSlug}/roles`

List all roles for the organization.

**Auth:** Authenticated org member

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Admin",
      "slug": "admin",
      "hierarchy": 10,
      "isDefault": false,
      "permissions": 110
    }
  ]
}
```

#### `POST /orgs/{orgSlug}/roles`

Create a new role. The caller can only create roles with a hierarchy number **higher** than their own.

**Auth:** `CanManageRoles`

**Request body:**

```json
{
  "name": "Volunteer Coordinator",
  "slug": "volunteer-coordinator",
  "hierarchy": 40,
  "permissions": 104
}
```

**Response:** `201 Created`

**Validation:**

- `hierarchy` must be greater than the caller's role hierarchy
- Cannot grant any permission bit that the caller's own role does not have

#### `PATCH /orgs/{orgSlug}/roles/{roleId}`

Update a role. The caller can only edit roles with a hierarchy number higher than their own.

**Auth:** `CanManageRoles`

**Request body:** Partial update — only include fields to change.

**Response:** `200 OK` — updated role object

**Validation:** Same hierarchy and permission constraints as creation.

#### `DELETE /orgs/{orgSlug}/roles/{roleId}`

Delete a role. Fails if any users are currently assigned to it.

**Auth:** `CanManageRoles` + caller's hierarchy must be lower than the target role

**Response:** `204 No Content`

**Error:** `409 Conflict` if the role has assigned members.

---

### Members

Manage org membership and user metadata.

#### `GET /orgs/{orgSlug}/members`

List organization members.

**Auth:** `CanViewAttendees`

**Query params:** `?roleId={uuid}` `?search={query}` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "userId": "uuid",
      "email": "user@example.com",
      "firstName": "Alex",
      "lastName": "Johnson",
      "role": { "id": "uuid", "slug": "member", "name": "Member" },
      "metadata": {
        "username": "FragMaster99"
      },
      "joinedAt": "2026-01-20T00:00:00Z"
    }
  ],
  "pagination": { "nextCursor": "abc", "hasMore": true }
}
```

#### `GET /orgs/{orgSlug}/members/search`

Search for members by email, name, or public metadata values (e.g., username). Used by the checkout UI to find attendees when buying tickets for others. Returns a short list of matches — not paginated, capped at 10 results.

**Auth:** Authenticated org member

**Query params:** `?q={search term}` (minimum 2 characters)

**Response:** `200 OK`

```json
{
  "data": [
    {
      "userId": "uuid",
      "firstName": "Alex",
      "lastName": "Johnson",
      "metadata": {
        "username": "FragMaster99"
      }
    },
    {
      "userId": "uuid",
      "firstName": "Jordan",
      "lastName": "Frag",
      "metadata": {
        "username": "JordanPlays"
      }
    }
  ]
}
```

**Notes:**

- Searches across `email`, `FirstName`, `LastName`, and all `UserOrganizationMetadataValues` where `IsPublic = true`
- Does **not** return email in the response — only enough info to identify the right person (name + public metadata). This prevents using the search endpoint to harvest email addresses
- Case-insensitive partial match

#### `PATCH /orgs/{orgSlug}/members/{userId}/role`

Change a member's role. The caller can only assign roles with a hierarchy number higher than their own, and can only modify members whose current role is lower-privilege than the caller's.

**Auth:** `CanManageRoles`

**Request body:**

```json
{
  "roleId": "uuid"
}
```

**Response:** `200 OK`

**Validation:**

- Target role hierarchy must be greater than caller's hierarchy
- Member's current role hierarchy must be greater than caller's hierarchy
- Cannot modify own role

#### `DELETE /orgs/{orgSlug}/members/{userId}`

Remove a member from the organization.

**Auth:** `CanManageRoles` + hierarchy check

**Response:** `204 No Content`

#### `GET /orgs/{orgSlug}/members/{userId}/metadata`

Get a member's org-specific metadata values.

**Auth:** Authenticated org member (own metadata) or `CanViewAttendees` (others)

**Response:** `200 OK`

```json
{
  "data": [
    {
      "metadataName": "username",
      "displayLabel": "Username",
      "value": "FragMaster99",
      "isPublic": true
    },
    {
      "metadataName": "discord_handle",
      "displayLabel": "Discord Handle",
      "value": "frag#1234",
      "isPublic": false
    }
  ]
}
```

#### `PUT /orgs/{orgSlug}/members/{userId}/metadata`

Set a member's org-specific metadata values. Replaces all values for the user in this org.

**Auth:** Authenticated (own metadata) or `CanManageRoles` (others)

**Request body:**

```json
{
  "values": {
    "username": "FragMaster99",
    "discord_handle": "frag#1234"
  }
}
```

**Response:** `200 OK`

**Validation:** Validates against `UserOrganizationMetadataInfo` — rejects unknown fields, enforces required fields.

---

### Metadata Definitions

Manage the custom profile field schema for an organization.

#### `GET /orgs/{orgSlug}/metadata`

List all metadata field definitions for the org.

**Auth:** Authenticated org member

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "metadataName": "username",
      "displayLabel": "Username",
      "isRequired": true,
      "isPublic": true
    }
  ]
}
```

#### `POST /orgs/{orgSlug}/metadata`

Create a new metadata field definition.

**Auth:** `CanManageOrganization`

**Request body:**

```json
{
  "metadataName": "switch_friend_code",
  "displayLabel": "Nintendo Switch Friend Code",
  "isRequired": false,
  "isPublic": true
}
```

**Response:** `201 Created`

#### `PATCH /orgs/{orgSlug}/metadata/{metadataId}`

Update a metadata field definition.

**Auth:** `CanManageOrganization`

**Response:** `200 OK`

#### `DELETE /orgs/{orgSlug}/metadata/{metadataId}`

Delete a metadata field definition and all associated values.

**Auth:** `CanManageOrganization`

**Response:** `204 No Content`

---

### Venues

#### `GET /orgs/{orgSlug}/venues`

List all venues for the organization.

**Auth:** Authenticated org member

**Query params:** `?search={query}` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Bartle Hall",
      "address": "301 W 13th St",
      "city": "Kansas City",
      "state": "MO",
      "zip": "64105",
      "capacity": 2000,
      "mapUrl": "https://...",
      "websiteUrl": "https://..."
    }
  ]
}
```

#### `POST /orgs/{orgSlug}/venues`

Create a new venue.

**Auth:** `CanCreateEvents` or `CanManageEvents`

**Request body:**

```json
{
  "name": "Bartle Hall",
  "address": "301 W 13th St",
  "city": "Kansas City",
  "state": "MO",
  "zip": "64105",
  "capacity": 2000,
  "notes": "Load-in through south entrance. Free parking in attached garage.",
  "websiteUrl": "https://..."
}
```

**Response:** `201 Created`

#### `PATCH /orgs/{orgSlug}/venues/{venueId}`

Update a venue. Changes are reflected for all future events using this venue. Existing events retain the venue reference, so their details update too — this is intentional (address corrections propagate).

**Auth:** `CanManageEvents`

**Response:** `200 OK`

#### `DELETE /orgs/{orgSlug}/venues/{venueId}`

Delete a venue. Fails if any events reference it.

**Auth:** `CanManageEvents`

**Response:** `204 No Content`

**Error:** `409 Conflict` if venue is in use.

---

### Events

#### `GET /orgs/{orgSlug}/events`

List events for the organization. Public endpoint returns only published, non-private events. Authenticated org members with `CanManageEvents` see drafts and private events.

**Auth:** Public (filtered) or Authenticated

**Query params:** `?status=upcoming,past,draft` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "name": "KCGameOn Summer 2026",
      "slug": "summer-2026",
      "shortDescription": "Kansas City's premier gaming convention",
      "startsAt": "2026-07-15T10:00:00Z",
      "endsAt": "2026-07-16T22:00:00Z",
      "isPublished": true,
      "isPrivate": false,
      "bannerUrl": "https://...",
      "avatarUrl": "https://...",
      "venue": {
        "id": "uuid",
        "name": "Bartle Hall",
        "address": "301 W 13th St",
        "city": "Kansas City",
        "state": "MO"
      }
    }
  ],
  "pagination": { "nextCursor": "abc", "hasMore": false }
}
```

#### `GET /orgs/{orgSlug}/events/{eventSlug}`

Get full event details including ticket offerings.

**Auth:** Public (if published) or `CanManageEvents` (drafts)

**Response:** `200 OK`

```json
{
  "id": "uuid",
  "name": "KCGameOn Summer 2026",
  "slug": "summer-2026",
  "description": "Full markdown/HTML description...",
  "shortDescription": "Kansas City's premier gaming convention",
  "startsAt": "2026-07-15T10:00:00Z",
  "endsAt": "2026-07-16T22:00:00Z",
  "isPublished": true,
  "isPrivate": false,
  "checkinStartsAt": "2026-07-15T08:00:00Z",
  "checkinEndsAt": "2026-07-16T23:00:00Z",
  "bannerUrl": "https://...",
  "venue": {
    "id": "uuid",
    "name": "Bartle Hall",
    "address": "301 W 13th St",
    "city": "Kansas City",
    "state": "MO",
    "zip": "64105"
  },
  "tickets": [
    {
      "id": "uuid",
      "ticketType": { "id": "uuid", "name": "General Admission" },
      "priceCents": 2500,
      "maxQuantity": 500,
      "soldCount": 213,
      "salesStartAt": "2026-05-01T00:00:00Z",
      "salesEndAt": "2026-07-15T10:00:00Z",
      "isActive": true,
      "requireAllDependencies": false,
      "dependencies": []
    },
    {
      "id": "uuid",
      "ticketType": { "id": "uuid", "name": "Food Add-On" },
      "priceCents": 1500,
      "maxQuantity": null,
      "soldCount": 87,
      "salesStartAt": null,
      "salesEndAt": null,
      "isActive": true,
      "requireAllDependencies": false,
      "dependencies": [
        { "eventTicketId": "uuid", "ticketTypeName": "General Admission" },
        { "eventTicketId": "uuid", "ticketTypeName": "VIP" }
      ]
    }
  ]
}
```

#### `POST /orgs/{orgSlug}/events`

Create a new event.

**Auth:** `CanCreateEvents`

**Request body:**

```json
{
  "name": "KCGameOn Summer 2026",
  "slug": "summer-2026",
  "description": "Full description in markdown...",
  "shortDescription": "Kansas City's premier gaming convention",
  "venueId": "uuid",
  "startsAt": "2026-07-15T10:00:00Z",
  "endsAt": "2026-07-16T22:00:00Z",
  "checkinStartsAt": "2026-07-15T08:00:00Z",
  "checkinEndsAt": "2026-07-16T23:00:00Z",
  "isPublished": false,
  "isPrivate": false,
  "bannerUrl": "https://..."
}
```

**Response:** `201 Created`

#### `PATCH /orgs/{orgSlug}/events/{eventSlug}`

Update an event.

**Auth:** `CanManageEvents`

**Request body:** Partial update.

**Response:** `200 OK`

#### `DELETE /orgs/{orgSlug}/events/{eventSlug}`

Delete an event. Fails if any orders exist for the event.

**Auth:** `CanManageEvents`

**Response:** `204 No Content`

**Error:** `409 Conflict` if orders exist. Suggest cancelling the event instead.

---

### Ticket Types

Org-level reusable ticket category templates.

#### `GET /orgs/{orgSlug}/ticket-types`

List ticket types for the org.

**Auth:** Authenticated org member

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "name": "General Admission",
      "description": "Full access to the event floor",
      "isActive": true
    }
  ]
}
```

#### `POST /orgs/{orgSlug}/ticket-types`

Create a ticket type template.

**Auth:** `CanManageEvents`

**Request body:**

```json
{
  "name": "General Admission",
  "description": "Full access to the event floor"
}
```

**Response:** `201 Created`

#### `PATCH /orgs/{orgSlug}/ticket-types/{ticketTypeId}`

Update a ticket type template.

**Auth:** `CanManageEvents`

**Response:** `200 OK`

---

### Event Tickets

Event-specific ticket offerings with pricing, capacity, and dependencies.

#### `GET /orgs/{orgSlug}/events/{eventSlug}/tickets`

List ticket offerings for an event. Public endpoint shows only active tickets with availability info. Admin view includes sold counts and inactive tickets.

**Auth:** Public (filtered) or `CanManageEvents` (full)

**Response:** `200 OK` — same structure as the `tickets` array in the event detail response.

#### `POST /orgs/{orgSlug}/events/{eventSlug}/tickets`

Create a ticket offering for an event.

**Auth:** `CanManageEvents`

**Request body:**

```json
{
  "ticketTypeId": "uuid",
  "priceCents": 2500,
  "maxQuantity": 500,
  "salesStartAt": "2026-05-01T00:00:00Z",
  "salesEndAt": "2026-07-15T10:00:00Z",
  "requireAllDependencies": false,
  "dependencyEventTicketIds": []
}
```

**Response:** `201 Created`

#### `PATCH /orgs/{orgSlug}/events/{eventSlug}/tickets/{eventTicketId}`

Update a ticket offering. Price changes only affect future orders.

**Auth:** `CanManageEvents`

**Request body:** Partial update. `dependencyEventTicketIds` replaces the full dependency set when provided.

**Response:** `200 OK`

#### `DELETE /orgs/{orgSlug}/events/{eventSlug}/tickets/{eventTicketId}`

Deactivate a ticket offering. Sets `IsActive = false` rather than deleting, since existing orders reference it.

**Auth:** `CanManageEvents`

**Response:** `204 No Content`

---

### Orders and Checkout

#### `POST /orgs/{orgSlug}/events/{eventSlug}/orders/checkout`

Initiate a ticket purchase. Validates availability, dependencies, and discount codes, then creates a Stripe Checkout Session via Stripe Connect. Returns the Stripe URL for redirect.

Each item in the `items` array represents one ticket for one attendee. Provide either `attendeeUserId` (known user) or `attendeeEmail` (gift to someone who may or may not have an account).

**Auth:** Authenticated

**Request body:**

```json
{
  "items": [
    {
      "eventTicketId": "uuid",
      "attendeeUserId": "uuid-alice"
    },
    {
      "eventTicketId": "uuid",
      "attendeeUserId": "uuid-bob"
    },
    {
      "eventTicketId": "uuid",
      "attendeeEmail": "charlie@example.com"
    },
    {
      "eventTicketId": "uuid-food-addon",
      "attendeeUserId": "uuid-alice"
    }
  ],
  "discountCode": "SUMMER20"
}
```

**Response:** `200 OK`

```json
{
  "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_...",
  "sessionId": "cs_...",
  "expiresAt": "2026-06-01T12:30:00Z"
}
```

**Attendee resolution rules:**

- `attendeeUserId` — ticket is assigned directly to this user on payment. Status becomes `valid`
- `attendeeEmail` matching an existing user — auto-resolved to their `UserId`, same as above
- `attendeeEmail` not matching any user — ticket is created with `status = 'pendingClaim'`, `UserId` is NULL, and `ClaimEmail` is set. A claim email is sent to the recipient with a link to create an account and claim the ticket
- `attendeeUserId` and `attendeeEmail` are mutually exclusive — provide one per item
- To buy for yourself, the UI auto-fills your own `UserId` as `attendeeUserId`

**Validation:**

- Each `eventTicketId` must be active and within its sales window
- Total items per `eventTicketId` must not exceed remaining availability (`maxQuantity - soldCount`)
- Dependency check: for each item, the **attendee** must hold (or be receiving in this cart) a valid prerequisite ticket. Respects `RequireAllDependencies` flag. For `attendeeEmail` items where the recipient has no account, dependency validation only checks other items in the same cart — existing tickets cannot be verified
- Discount code must be valid, active, within usage limits, not expired, and scoped to this event/ticket
- Org must have completed Stripe Connect onboarding

**Notes:**

- The discount code is looked up by the `code` string but stored on the Order as `DiscountCodeId` (FK)
- The Order record is created in `pending` status. It transitions to `completed` only when the Stripe webhook confirms payment
- Items for the same `eventTicketId` are grouped into a single Stripe line item for a clean checkout page (e.g., "General Admission × 3")

#### `POST /orgs/{orgSlug}/events/{eventSlug}/orders/webhook`

Stripe webhook endpoint. Handles `checkout.session.completed`, `checkout.session.expired`, and `charge.refunded` events.

**Auth:** Stripe webhook signature verification (not user auth)

**`checkout.session.completed`:**

1. Find the pending Order by `stripe_checkout_session_id`
2. Update Order status to `completed`
3. Create UserTicket records for each item with generated `TicketCode`:
   - If `attendeeUserId` is set: `status = 'valid'`, `UserId` is set
   - If `attendeeEmail` with no matching user: `status = 'pendingClaim'`, `UserId` is NULL, `ClaimEmail` and `ClaimToken` are set
4. Increment `TimesUsed` on the DiscountCode if applicable
5. Send confirmation emails:
   - To the purchaser: order receipt with all ticket details
   - To each known attendee (where `UserId` is set): their QR code
   - To each `ClaimEmail` (pending claim): claim link to create account and claim ticket

**`checkout.session.expired`:**

1. Update Order status to `failed`

**`charge.refunded`:**

1. Update Order status to `refunded`
2. Update associated UserTickets status to `cancelled`

**Response:** `200 OK` (acknowledge receipt to Stripe)

#### `GET /orgs/{orgSlug}/events/{eventSlug}/orders`

List orders for an event.

**Auth:** `CanViewAttendees`

**Query params:** `?status=completed` `?search={email or name}` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "purchaser": {
        "userId": "uuid",
        "email": "user@example.com",
        "firstName": "Alex",
        "lastName": "Johnson"
      },
      "status": "completed",
      "totalCents": 5000,
      "platformFeeCents": 150,
      "discountCode": { "id": "uuid", "code": "SUMMER20" },
      "items": [
        {
          "ticketType": "General Admission",
          "priceCents": 2500,
          "attendee": {
            "userId": "uuid",
            "firstName": "Alex",
            "lastName": "Johnson"
          },
          "status": "checkedIn"
        },
        {
          "ticketType": "General Admission",
          "priceCents": 2500,
          "attendee": null,
          "claimEmail": "charlie@example.com",
          "status": "pendingClaim"
        }
      ],
      "createdAt": "2026-06-01T12:00:00Z"
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false }
}
```

#### `POST /orgs/{orgSlug}/events/{eventSlug}/orders/{orderId}/refund`

Initiate a refund through Stripe.

**Auth:** `CanManageBilling`

**Request body:**

```json
{
  "reason": "Customer requested cancellation"
}
```

**Response:** `200 OK`

```json
{
  "orderId": "uuid",
  "status": "refunded",
  "stripeRefundId": "re_..."
}
```

---

### Check-In

#### `POST /orgs/{orgSlug}/events/{eventSlug}/checkin`

Scan a QR code and check in an attendee.

**Auth:** `CanCheckin`

**Request body:**

```json
{
  "ticketCode": "TK-abc123"
}
```

**Response (success):** `200 OK`

```json
{
  "status": "checkedIn",
  "ticket": {
    "id": "uuid",
    "ticketCode": "TK-abc123",
    "ticketType": "General Admission",
    "checkedInAt": "2026-07-15T09:32:00Z"
  },
  "attendee": {
    "userId": "uuid",
    "firstName": "Alex",
    "lastName": "Johnson",
    "metadata": {
      "username": "FragMaster99"
    }
  }
}
```

**Response (already checked in):** `200 OK`

```json
{
  "status": "alreadyCheckedIn",
  "ticket": {
    "id": "uuid",
    "ticketCode": "TK-abc123",
    "ticketType": "General Admission",
    "checkedInAt": "2026-07-15T09:15:00Z",
    "checkedInBy": "Staff Member Name"
  },
  "attendee": {
    "userId": "uuid",
    "firstName": "Alex",
    "lastName": "Johnson",
    "metadata": { "username": "FragMaster99" }
  }
}
```

**Response (invalid):** `422 Unprocessable Entity`

```json
{
  "error": {
    "code": "TICKET_INVALID",
    "message": "This ticket is cancelled or does not exist for this event."
  }
}
```

**Response (pending claim):** `422 Unprocessable Entity`

```json
{
  "error": {
    "code": "TICKET_PENDING_CLAIM",
    "message": "This ticket has not been claimed yet. The recipient needs to claim it before check-in.",
    "details": {
      "claimEmail": "char***@example.com",
      "purchasedBy": "Alex Johnson"
    }
  }
}
```

**Response (check-in closed):** `422 Unprocessable Entity`

```json
{
  "error": {
    "code": "CHECKIN_WINDOW_CLOSED",
    "message": "Check-in is not currently open for this event.",
    "details": {
      "checkinStartsAt": "2026-07-15T08:00:00Z",
      "checkinEndsAt": "2026-07-16T23:00:00Z"
    }
  }
}
```

**Metadata note:** Only fields marked `IsPublic = true` in the org's metadata definitions are returned in the check-in response. This ensures private info like emails or phone numbers are not displayed to door staff unless the org has explicitly flagged them as public.

#### `GET /orgs/{orgSlug}/events/{eventSlug}/checkin/stats`

Real-time check-in statistics.

**Auth:** `CanCheckin` or `CanViewAttendees`

**Response:** `200 OK`

```json
{
  "totalTickets": 426,
  "checkedIn": 312,
  "remaining": 98,
  "pendingClaim": 16,
  "byTicketType": [
    {
      "ticketType": "General Admission",
      "total": 350,
      "checkedIn": 270,
      "pendingClaim": 12
    },
    { "ticketType": "VIP", "total": 76, "checkedIn": 42, "pendingClaim": 4 }
  ]
}
```

---

### Discount Codes

#### `GET /orgs/{orgSlug}/discount-codes`

List discount codes for the org.

**Auth:** `CanManageEvents`

**Query params:** `?eventId={uuid}` `?isActive=true` `?limit=25` `?cursor=...`

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "uuid",
      "code": "SUMMER20",
      "discountCents": 0,
      "discountPercent": 20,
      "maxUses": 100,
      "timesUsed": 34,
      "isActive": true,
      "expiresAt": "2026-07-01T00:00:00Z",
      "event": { "id": "uuid", "name": "KCGameOn Summer 2026" },
      "eventTicket": null,
      "owner": {
        "userId": "uuid",
        "firstName": "Alex",
        "lastName": "Johnson",
        "metadata": { "username": "FragMaster99" }
      }
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false }
}
```

#### `POST /orgs/{orgSlug}/discount-codes`

Create a discount code.

**Auth:** `CanManageEvents`

**Request body:**

```json
{
  "code": "SUMMER20",
  "eventId": "uuid",
  "eventTicketId": null,
  "userId": "uuid",
  "discountPercent": 20,
  "maxUses": 100,
  "expiresAt": "2026-07-01T00:00:00Z"
}
```

**Response:** `201 Created`

**Validation:**

- `discountCents` and `discountPercent` are mutually exclusive — provide one, not both
- `code` must be unique within the org
- `eventId` and `eventTicketId` must belong to this org

#### `PATCH /orgs/{orgSlug}/discount-codes/{discountCodeId}`

Update a discount code.

**Auth:** `CanManageEvents`

**Response:** `200 OK`

#### `DELETE /orgs/{orgSlug}/discount-codes/{discountCodeId}`

Deactivate a discount code. Sets `IsActive = false` rather than deleting since orders reference it.

**Auth:** `CanManageEvents`

**Response:** `204 No Content`

---

### Ticket Claims

Handles the flow for tickets purchased for someone who doesn't have an account yet. The recipient receives an email with a claim link containing a `ClaimToken`.

#### `GET /tickets/claim/{claimToken}`

Look up a pending claim ticket by its claim token. Used by the claim page to show ticket details before the recipient creates an account or logs in.

**Auth:** Public

**Response:** `200 OK`

```json
{
  "ticketCode": "TK-abc123",
  "ticketType": "General Admission",
  "event": {
    "name": "KCGameOn Summer 2026",
    "startsAt": "2026-07-15T10:00:00Z",
    "venue": {
      "name": "Bartle Hall",
      "address": "301 W 13th St, Kansas City, MO"
    }
  },
  "organization": {
    "slug": "kcgameon",
    "name": "KCGameOn"
  },
  "purchasedBy": "Alex Johnson",
  "claimEmail": "charlie@example.com",
  "claimExpiresAt": "2026-07-15T10:00:00Z",
  "status": "pendingClaim"
}
```

**Error:** `404` if token is invalid, `410 Gone` if claim has expired.

#### `POST /tickets/claim/{claimToken}`

Claim a pending ticket. The authenticated user becomes the ticket holder. If the org has required metadata fields, the claim response indicates they need to fill those out separately.

**Auth:** Authenticated

**Request body:** None (the authenticated user's identity is used)

**Response:** `200 OK`

```json
{
  "ticket": {
    "id": "uuid",
    "ticketCode": "TK-abc123",
    "status": "valid",
    "qrCodeUrl": "https://api.yourplatform.com/tickets/TK-abc123/qr"
  },
  "requiresMetadata": true,
  "metadataUrl": "https://yourplatform.com/orgs/kcgameon/profile"
}
```

**Side effects:**

- Sets `UserId` on the UserTicket to the authenticated user
- Updates status from `pendingClaim` to `valid`
- Clears `ClaimToken` and `ClaimEmail`
- If the user is not already a member of the org, creates a `UserOrganizations` record with the default role
- If the org has required metadata fields (e.g., KCGameOn username), sets `requiresMetadata = true` so the UI can prompt them to fill out their profile

**Validation:**

- Token must be valid and not expired
- Ticket must be in `pendingClaim` status
- The authenticated user's email should match `ClaimEmail` — if it doesn't, still allow the claim but log a warning (someone may have forwarded the link, which is fine)

#### `POST /tickets/claim/{claimToken}/resend`

Resend the claim email. Available to the original purchaser in case the recipient didn't receive it or the email was wrong.

**Auth:** Authenticated (must be the purchaser on the associated Order)

**Request body:**

```json
{
  "email": "charlie.new@example.com"
}
```

**Response:** `200 OK`

```json
{
  "message": "Claim email sent.",
  "claimEmail": "charlie.new@example.com",
  "claimExpiresAt": "2026-07-15T10:00:00Z"
}
```

**Notes:**

- If `email` is provided and differs from the original `ClaimEmail`, updates the claim email and generates a new `ClaimToken` (invalidating the old one)
- If `email` is omitted, resends to the original `ClaimEmail` with the same token
- Rate limited to prevent abuse

---

### Tickets (QR Code)

#### `GET /tickets/{ticketCode}/qr`

Generate a QR code image for a ticket. Returns a PNG image.

**Auth:** Authenticated (must be the ticket holder or purchaser)

**Response:** `200 OK` with `Content-Type: image/png`

---

## Middleware Pipeline

Aspire ServiceDefaults are applied first via `builder.AddServiceDefaults()`, which adds OpenTelemetry, health checks, HTTP resilience, and service discovery. The custom middleware layers on top:

```txt
Request
  │
  ├─ 0. Aspire ServiceDefaults (OpenTelemetry, resilience, service discovery)
  ├─ 1. Exception Handler
  ├─ 2. CORS
  ├─ 3. Rate Limiting
  ├─ 4. Firebase JWT Validation
  ├─ 5. Org Resolution (resolve orgSlug → OrganizationId, attach to HttpContext)
  ├─ 6. Membership Resolution (lookup UserOrganizations for current user + org)
  ├─ 7. Permission Filter (check role permission bit against endpoint requirement)
  │
  └─ Endpoint Handler

Health endpoints (added by Aspire):
  /health  → readiness probe
  /alive   → liveness probe
```

### Org Resolution Middleware

Runs on all `/orgs/{orgSlug}/*` routes. Resolves the slug to an org record and attaches it to `HttpContext.Items`. Returns `404` if the org doesn't exist.

### Membership Resolution Middleware

Runs after org resolution for authenticated requests. Looks up the user's `UserOrganizations` record for the current org and loads the role's bitwise `Permissions` value into `CurrentUserContext`. If the user is not a member of the org, permissions remain `None` — individual endpoint filters decide whether to allow or deny.

### Permission Filter

Applied per-endpoint via `.RequirePermission("CanManageEvents")`. Reads `CurrentUserContext` and checks the specified permission bit via `HasPermission()` (which uses `HasFlag()`). Returns `403` if the bit is not set or the user is not a member.

```csharp
// Example registration
public static class PermissionExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var role = context.HttpContext.GetOrgRole();
            if (role is null || !role.HasPermission(permission))
                return Results.Forbid();
            return await next(context);
        });
    }
}
```

---

## Webhook Security

Stripe webhooks are verified using the webhook signing secret. The endpoint does not use Firebase JWT auth — it uses Stripe's signature verification instead.

```csharp
app.MapPost("/orgs/{orgSlug}/events/{eventSlug}/orders/webhook", async (
    HttpRequest request,
    IStripeWebhookService stripeService) =>
{
    var json = await new StreamReader(request.Body).ReadToEndAsync();
    var signature = request.Headers["Stripe-Signature"];

    var stripeEvent = stripeService.VerifyAndParse(json, signature);
    // ... handle event
});
```

---

## Project Structure

See [project-structure.md](./project-structure.md) for the full monorepo layout, frontend architecture, naming conventions, and local development setup.

---

## Key Implementation Notes

### Ticket Availability (Race Conditions)

Use a database-level check to prevent overselling. When creating an order, use a transaction with a `SELECT ... FOR UPDATE` (or EF Core pessimistic concurrency) on the EventTicket row to lock it while checking availability. Count the number of items in the cart for each `EventTicketId` against remaining capacity.

```csharp
// Pseudocode for availability check inside a transaction
await using var transaction = await dbContext.Database
    .BeginTransactionAsync(IsolationLevel.Serializable);

// Group checkout items by EventTicketId and count per type
var itemCounts = checkoutRequest.Items
    .GroupBy(i => i.EventTicketId)
    .ToDictionary(g => g.Key, g => g.Count());

foreach (var (eventTicketId, requestedCount) in itemCounts)
{
    // SQL Server: use UPDLOCK + HOLDLOCK to prevent concurrent reads
    var eventTicket = await dbContext.EventTickets
        .FromSqlRaw(
            "SELECT * FROM EventTickets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {0}",
            eventTicketId)
        .FirstAsync();

    var soldCount = await dbContext.UserTickets
        .CountAsync(ut => ut.OrderItem.EventTicketId == eventTicketId
            && ut.Status != "cancelled");

    if (eventTicket.MaxQuantity.HasValue
        && soldCount + requestedCount > eventTicket.MaxQuantity)
        throw new TicketSoldOutException(eventTicketId);
}
```

### Dependency Validation

When validating ticket dependencies at checkout, collect all `EventTicketId` values from the current cart and the attendee's existing valid UserTickets. Then for each item with dependencies, check:

- If `RequireAllDependencies = false`: attendee has at least one prerequisite
- If `RequireAllDependencies = true`: attendee has all prerequisites

Dependencies are checked per-attendee, not per-purchaser. For items where the attendee is identified by `attendeeEmail` with no existing account, dependencies can only be validated against other items in the same cart — the system cannot check existing tickets for a user that doesn't exist yet.

### Ticket Claim Flow

When a ticket is purchased for an `attendeeEmail` that doesn't match any existing user:

1. **At webhook processing:** Create the UserTicket with `UserId = NULL`, `Status = 'pendingClaim'`, `ClaimEmail` set to the recipient's email, and `ClaimToken` set to a cryptographically random URL-safe string. Set `ClaimExpiresAt` to the event's `StartsAt` timestamp (or 30 days from purchase, whichever is sooner).

2. **Claim email:** Send an email to `ClaimEmail` containing a link like `https://yourplatform.com/claim/{claimToken}`. The landing page shows ticket details and prompts the recipient to log in or create an account.

3. **Claiming:** When the recipient hits `POST /tickets/claim/{claimToken}`, assign the ticket to their `UserId`, set status to `valid`, clear the claim fields, and auto-create an org membership if needed.

4. **Expiration:** A background job (or lazy check on access) marks expired unclaimed tickets as `claimExpired`. The recipient can no longer self-claim after expiration. The ticket remains in `pendingClaim` status and cannot be scanned at check-in — the purchaser would need to contact an org admin to manually reassign the ticket.

5. **Resend / update email:** The purchaser can hit `POST /tickets/claim/{claimToken}/resend` to resend or change the claim email. Changing the email invalidates the old token and generates a new one.

**Edge case — purchaser shows up with an unclaimed ticket:** At check-in, staff will see the ticket is `pendingClaim` and the system rejects the scan. An admin with `CanManageEvents` should have the ability to manually assign the ticket to a user or override the check-in via a separate admin endpoint (not yet defined — add when needed).

### Stripe Connect Considerations

- Use `payment_intent_data.application_fee_amount` on the Checkout Session to set the platform fee
- Use `Stripe-Account` header (or `stripe_account` param in the SDK) for all API calls on behalf of the connected account
- Store both the Checkout Session ID and the Payment Intent ID on the Order — the session is needed for the redirect flow, the PI is needed for refunds

### QR Code Generation

Generate `TicketCode` as a short, URL-safe unique string (e.g., nanoid or a prefixed UUID segment like `TK-a3bF9x`). Encode as a QR code pointing to a short URL like `https://yourplatform.com/t/TK-a3bF9x`. The check-in web page can either scan the QR or accept manual entry of the code.
