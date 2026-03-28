# Database Design — Multi-Tenant Ticketing Platform

## Overview

This document defines the database schema for a multi-tenant event ticketing platform. The platform allows multiple organizations to manage events, sell tickets via Stripe Connect, and check in attendees via QR code. Each organization can define custom user profile fields (e.g., KCGameOn requires usernames) without schema changes.

Authentication is handled externally by Firebase. The `AuthProviderId` on the Users table stores the Firebase user identifier.

---

## Tables

### Organizations

The top-level tenant. Every event, ticket type, and custom metadata field belongs to an organization.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| Name | NVARCHAR(100) | NOT NULL | Display name |
| Slug | NVARCHAR(100) | NOT NULL, UNIQUE | URL-safe identifier, e.g. `kcgameon` |
| StripeConnectAccountId | NVARCHAR(255) | NULLABLE | Stripe connected account ID, null until onboarded |
| LogoUrl | NVARCHAR(500) | NULLABLE | |
| WebsiteUrl | NVARCHAR(500) | NULLABLE | |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### Users

Core identity. Auth credentials live in Firebase — this table holds profile data and the link to Firebase.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| AuthProviderId | NVARCHAR(255) | NOT NULL, UNIQUE | Firebase UID |
| Email | NVARCHAR(255) | NOT NULL, UNIQUE | |
| FirstName | NVARCHAR(100) | NOT NULL | |
| LastName | NVARCHAR(100) | NOT NULL | |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### Roles

Organization-defined roles for access control. Each org can create whatever roles make sense for them. Permissions use a bitwise flags pattern (Discord-style) — a single `BIGINT` column where each bit represents a permission. New permissions are added by appending the next bit position; positions must never be reused or reordered. The `hierarchy` column enforces a pecking order: users with `CanManageRoles` can only create, edit, or assign roles with a **higher** hierarchy number (lower privilege) than their own.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| Name | NVARCHAR(100) | NOT NULL | Display name, e.g. `Admin`, `Volunteer`, `Moderator` |
| Slug | NVARCHAR(100) | NOT NULL | Code-safe identifier, e.g. `admin`, `volunteer` |
| Hierarchy | INT | NOT NULL, DEFAULT 100 | Lower number = higher privilege. Used to restrict role management |
| IsDefault | BIT | NOT NULL, DEFAULT 0 | Auto-assigned to new members of this org |
| Permissions | BIGINT | NOT NULL, DEFAULT 0 | Bitwise permission flags (see Permission Bits below) |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Permission Bits:**

| Bit | Value | Permission | Description |
| --- | --- | --- | --- |
| 0 | 1 | CanManageOrganization | Edit org branding, description, settings |
| 1 | 2 | CanCreateEvents | Create new events |
| 2 | 4 | CanManageEvents | Edit/delete existing events and ticket types |
| 3 | 8 | CanManageRoles | Create/edit/assign roles with higher hierarchy than own role |
| 4 | 16 | CanManageBilling | Stripe Connect, view financials |
| 5 | 32 | CanCheckin | Scan QR codes at the door |
| 6 | 64 | CanViewAttendees | View attendee lists and sales |

**Unique constraint:** `(OrganizationId, Slug)`

**Hierarchy enforcement:** A user whose role has `hierarchy = 20` and `CanManageRoles = true` can only manage roles where `hierarchy > 20`. They cannot edit their own role or any role at or above their level. This prevents a volunteer coordinator from escalating their own privileges or editing admin roles.

**Example roles for KCGameOn:**

| Name | Slug | Hierarchy | IsDefault | Permissions (decimal) | Permissions (flags) |
| --- | --- | --- | --- | --- | --- |
| Owner | owner | 0 | FALSE | 127 | All 7 permissions |
| Admin | admin | 10 | FALSE | 110 | CanCreateEvents \| CanManageEvents \| CanManageRoles \| CanCheckin \| CanViewAttendees |
| Moderator | moderator | 20 | FALSE | 104 | CanManageRoles \| CanCheckin \| CanViewAttendees |
| Volunteer | volunteer | 50 | FALSE | 32 | CanCheckin |
| Member | member | 100 | TRUE | 0 | None |

---

### UserOrganizations

Maps users to organizations with a role. Controls who can manage events, scan tickets, view dashboards, etc. A user can belong to multiple organizations.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| UserId | UNIQUEIDENTIFIER | FK → Users, NOT NULL | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| RoleId | UNIQUEIDENTIFIER | FK → Roles, NOT NULL | References the org's Roles table |
| StripeCustomerId | NVARCHAR(255) | NULL | Stripe Customer ID on the org's connected account |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(UserId, OrganizationId)`

---

### UserOrganizationMetadataInfo

Defines custom profile fields an organization requires or allows. This is the "schema" for org-specific user data. For example, KCGameOn would create a row here for "Username" and another for "DiscordHandle."

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| MetadataName | NVARCHAR(100) | NOT NULL | Field name, e.g. `username`, `discord_handle` |
| DisplayLabel | NVARCHAR(100) | NOT NULL | Shown to users, e.g. `Username`, `Discord Handle` |
| IsRequired | BIT | NOT NULL, DEFAULT 0 | Must the user fill this in? |
| IsPublic | BIT | NOT NULL, DEFAULT 0 | Visible to other users (e.g., in raffles) |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(OrganizationId, MetadataName)`

---

### UserOrganizationMetadataValues

Stores actual values for the custom fields defined above. One row per user per field per org. This is the EAV value table.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| UserId | UNIQUEIDENTIFIER | FK → Users, NOT NULL | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| MetadataInfoId | UNIQUEIDENTIFIER | FK → UserOrganizationMetadataInfo, NOT NULL | |
| MetadataValue | NVARCHAR(500) | NOT NULL | The user's value for this field |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(UserId, MetadataInfoId)`

**Example rows for KCGameOn:**

| UserId | OrganizationId | MetadataInfoId | MetadataValue |
| --- | --- | --- | --- |
| (alice) | (kcgameon) | (username field) | FragMaster99 |
| (alice) | (kcgameon) | (discord field) | frag#1234 |

---

### Venues

Reusable venue records scoped to an organization. Avoids retyping addresses and provides a single place to maintain venue-specific details.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| Name | NVARCHAR(200) | NOT NULL | e.g. `Bartle Hall`, `KCI Expo Center` |
| Address | NVARCHAR(500) | NOT NULL | Full street address |
| City | NVARCHAR(100) | NULLABLE | |
| State | NVARCHAR(50) | NULLABLE | |
| Zip | NVARCHAR(20) | NULLABLE | |
| Notes | NNVARCHAR(MAX) | NULLABLE | Parking info, load-in instructions, etc. |
| Capacity | INT | NULLABLE | Max attendees if known |
| MapUrl | NVARCHAR(500) | NULLABLE | Floor plan or venue map image |
| WebsiteUrl | NVARCHAR(500) | NULLABLE | |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### Events

An event belonging to an organization. Replaces the old KCGameOn `Events` table.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| VenueId | UNIQUEIDENTIFIER | FK → Venues, NULLABLE | NULL for virtual events |
| Name | NVARCHAR(200) | NOT NULL | |
| Slug | NVARCHAR(200) | NOT NULL | URL-safe, unique per org |
| Description | NNVARCHAR(MAX) | NULLABLE | Supports markdown or HTML |
| ShortDescription | NVARCHAR(500) | NULLABLE | Tagline / feature text |
| StartsAt | DATETIMEOFFSET | NOT NULL | |
| EndsAt | DATETIMEOFFSET | NULLABLE | |
| BannerUrl | NVARCHAR(500) | NULLABLE | |
| AvatarUrl | NVARCHAR(500) | NULLABLE | |
| IsPublished | BIT | NOT NULL, DEFAULT 0 | Replaces draft/active flags |
| IsPrivate | BIT | NOT NULL, DEFAULT 0 | Only accessible via direct link |
| CheckinStartsAt | DATETIMEOFFSET | NULLABLE | When QR check-in opens. NULL = no check-in |
| CheckinEndsAt | DATETIMEOFFSET | NULLABLE | When QR check-in closes. NULL = no auto-close |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(OrganizationId, Slug)`

---

### TicketTypes

Org-level templates for reusable ticket categories. These define the label and description but not event-specific pricing or capacity.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| Name | NVARCHAR(100) | NOT NULL | e.g. `General Admission`, `VIP`, `BYOC` |
| Description | NVARCHAR(1000) | NULLABLE | |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### EventTickets

Event-specific ticket offerings. Links a TicketType to an Event with pricing and capacity. This is what attendees actually purchase.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| EventId | UNIQUEIDENTIFIER | FK → Events, NOT NULL | |
| TicketTypeId | UNIQUEIDENTIFIER | FK → TicketTypes, NOT NULL | |
| PriceCents | INT | NOT NULL | Price in cents to avoid floating point |
| MaxQuantity | INT | NULLABLE | NULL = unlimited |
| SalesStartAt | DATETIMEOFFSET | NULLABLE | When tickets go on sale |
| SalesEndAt | DATETIMEOFFSET | NULLABLE | When sales close |
| RequireAllDependencies | BIT | NOT NULL, DEFAULT 0 | FALSE = any one prerequisite is enough. TRUE = all prerequisites required |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| StripeProductId | NVARCHAR(255) | NULLABLE | Stripe Product ID on the org's connected account |
| StripePriceId | NVARCHAR(255) | NULLABLE | Stripe Price ID on the org's connected account |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### EventTicketDependencies

Defines prerequisite tickets. Whether the attendee needs **any** or **all** of the listed prerequisites is controlled by `RequireAllDependencies` on the parent EventTicket.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| EventTicketId | UNIQUEIDENTIFIER | FK → EventTickets, NOT NULL | The ticket that has a prerequisite |
| RequiresEventTicketId | UNIQUEIDENTIFIER | FK → EventTickets, NOT NULL | A ticket that satisfies the requirement |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(EventTicketId, RequiresEventTicketId)`

**Example — Food Add-On requires GA or VIP (`RequireAllDependencies = false`):**

| EventTicketId | RequiresEventTicketId |
| --- | --- |
| (food-addon) | (general-admission) |
| (food-addon) | (vip) |

**Validation logic:** When a user attempts to purchase an EventTicket that has rows in this table, check the attendee's existing valid UserTickets (and items in the current cart) against the prerequisites. If `RequireAllDependencies = false`, any one match is sufficient. If `TRUE`, all prerequisites must be met. **For gift purchases, dependencies are checked against the recipient (the attendee on the UserTicket), not the purchaser.** The person receiving the Food Add-On must hold the GA/VIP ticket, not the person paying for it.

---

### Orders

Groups one or more purchased tickets into a single transaction. Tied to a Stripe Checkout Session or Payment Intent via Stripe Connect.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| UserId | UNIQUEIDENTIFIER | FK → Users, NOT NULL | The purchaser |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | Denormalized for easier querying |
| EventId | UNIQUEIDENTIFIER | FK → Events, NOT NULL | |
| StripePaymentIntentId | NVARCHAR(255) | NULLABLE | Stripe PI on the connected account |
| StripeCheckoutSessionId | NVARCHAR(255) | NULLABLE | Stripe Checkout Session ID |
| Status | NVARCHAR(50) | NOT NULL, DEFAULT 'pending' | `pending`, `completed`, `refunded`, `failed` |
| TotalCents | INT | NOT NULL | Total amount charged |
| PlatformFeeCents | INT | NOT NULL, DEFAULT 0 | Your platform's cut |
| DiscountCodeId | UNIQUEIDENTIFIER | FK → DiscountCodes, NULLABLE | Discount code applied at checkout |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |
| UpdatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### OrderItems

Individual line items within an order. One row per ticket per attendee — maps 1:1 to a UserTicket.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrderId | UNIQUEIDENTIFIER | FK → Orders, NOT NULL | |
| EventTicketId | UNIQUEIDENTIFIER | FK → EventTickets, NOT NULL | Which ticket offering was purchased |
| PriceCents | INT | NOT NULL | Price at time of purchase (snapshot) |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### UserTickets

The actual ticket assigned to an attendee. Separated from OrderItems to support buying tickets for others. This is what generates the QR code and gets scanned at the door. For gift tickets where the recipient doesn't have an account, `UserId` is NULL and the ticket enters `pendingClaim` status until claimed.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrderItemId | UNIQUEIDENTIFIER | FK → OrderItems, NOT NULL | Links back to the purchase |
| UserId | UNIQUEIDENTIFIER | FK → Users, NULLABLE | The attendee. NULL for unclaimed gift tickets |
| EventId | UNIQUEIDENTIFIER | FK → Events, NOT NULL | Denormalized for check-in queries |
| TicketCode | NVARCHAR(50) | NOT NULL, UNIQUE | Short unique code encoded in QR |
| Status | NVARCHAR(50) | NOT NULL, DEFAULT 'valid' | `valid`, `checkedIn`, `cancelled`, `transferred`, `pendingClaim` |
| ClaimEmail | NVARCHAR(255) | NULLABLE | Recipient email for unclaimed gift tickets |
| ClaimToken | NVARCHAR(255) | NULLABLE, UNIQUE | URL-safe token sent in claim email |
| ClaimExpiresAt | DATETIMEOFFSET | NULLABLE | When the claim link expires |
| CheckedInAt | DATETIMEOFFSET | NULLABLE | Timestamp of QR scan at the door |
| CheckedInByStaffId | UNIQUEIDENTIFIER | FK → Users, NULLABLE | Which staff member scanned it |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

---

### DiscountCodes

Promo codes that can be scoped to an entire event or a specific ticket offering. Optionally owned by a user (influencer/affiliate) for tracking referrals and commissions.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| Id | UNIQUEIDENTIFIER | PK | |
| OrganizationId | UNIQUEIDENTIFIER | FK → Organizations, NOT NULL | |
| EventId | UNIQUEIDENTIFIER | FK → Events, NULLABLE | NULL = applies to all org events |
| EventTicketId | UNIQUEIDENTIFIER | FK → EventTickets, NULLABLE | NULL = applies to all tickets in event |
| ReferrerUserId | UNIQUEIDENTIFIER | FK → Users, NULLABLE | The influencer/affiliate who gets credit when this code is used. NULL = org-wide promo |
| Code | NVARCHAR(100) | NOT NULL | |
| DiscountCents | INT | NOT NULL, DEFAULT 0 | Flat amount off (in cents) |
| DiscountPercent | INT | NOT NULL, DEFAULT 0 | Percentage off (0-100) |
| MaxUses | INT | NULLABLE | NULL = unlimited |
| TimesUsed | INT | NOT NULL, DEFAULT 0 | |
| ExpiresAt | DATETIMEOFFSET | NULLABLE | |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAt | DATETIMEOFFSET | NOT NULL, DEFAULT SYSDATETIMEOFFSET() | |

**Unique constraint:** `(OrganizationId, Code)`

---

## Relationships Diagram

```txt
Organizations
├── Roles
│   └── UserOrganizations ──→ Users
├── UserOrganizationMetadataInfo
│   └── UserOrganizationMetadataValues ──→ Users
├── Venues
├── TicketTypes
├── DiscountCodes ──→ Users (optional influencer/affiliate owner)
└── Events ──→ Venues
    └── EventTickets ──→ TicketTypes
        ├── EventTicketDependencies (self-referential)
        └── OrderItems
            ├── ──→ Orders ──→ Users (purchaser), DiscountCodes
            └── UserTickets ──→ Users (attendee)
```

---

## Key Design Decisions

### Multi-Tenancy via Organization ID

Every data-bearing table traces back to an organization. API queries should always scope by `OrganizationId` to prevent data leakage between tenants. Consider row-level security policies in SQL Server for defense-in-depth.

### EAV for Custom Profile Fields

The `UserOrganizationMetadataInfo` / `UserOrganizationMetadataValues` pair lets each organization define arbitrary user profile fields without schema migrations. This avoids the KCGameOn problem of stuffing `DiscordAccount`, `SteamHandle`, `PSN_ID`, etc., all into the user table. The tradeoff is that querying metadata is slightly more complex (joins or subqueries), but for profile display and form rendering it works well.

### Orders vs. UserTickets Separation

The purchaser (on Orders) and the attendee (on UserTickets) can be different people. This supports gifting tickets, buying for friends, and corporate group purchases. The `OrderItemId` on UserTickets links back to the financial record. For gifts to people without accounts, UserTickets are created with `UserId = NULL` and `Status = 'pendingClaim'`. The `ClaimEmail`, `ClaimToken`, and `ClaimExpiresAt` fields manage the flow of the recipient creating an account and claiming the ticket.

### Price Snapshot on OrderItems

`PriceCents` is stored on both `EventTickets` (the current price) and `OrderItems` (the price at time of purchase). If ticket prices change after someone buys, their order still reflects what they actually paid.

### Check-In Audit Trail

`UserTickets.CheckedInAt` and `CheckedInByStaffId` record exactly when someone was scanned in and by whom. The QR code encodes `TicketCode`, and the check-in API endpoint requires the staff member's role to have the `CanCheckin` permission bit set. Check-in is only available when the current time falls within the event's `CheckinStartsAt` and `CheckinEndsAt` window. If `CheckinStartsAt` is NULL, check-in is disabled for that event. Tickets in `pendingClaim` status cannot be checked in — they must be claimed first.

### Organization-Defined Roles

Roles are stored in the database rather than hardcoded so each organization can define roles that match their structure. Permissions use a bitwise flags pattern (Discord-style) — a single `BIGINT Permissions` column where each bit represents a capability. The C# `Permission` enum is `[Flags] : long` with power-of-2 values. New permissions are added by appending the next bit shift (`1L << 7`, `1L << 8`, etc.); existing roles are unaffected because their stored value doesn't include the new bit. Bit positions must never be reused or reordered. The `hierarchy` integer enforces a pecking order — a user can only manage or assign roles with a higher hierarchy number than their own. This prevents privilege escalation (e.g., a volunteer coordinator editing an admin role or assigning themselves to it).

### Venues Notes

Venues are a separate table rather than inline strings on Events because organizations frequently reuse the same venue. This ensures address consistency, lets orgs maintain venue-specific notes (parking, load-in, capacity), and simplifies event creation — select a venue from a dropdown instead of retyping. Virtual events set `VenueId` to NULL.

### Discount Code Ownership and Referral Tracking

The optional `ReferrerUserId` on `DiscountCodes` supports influencer and affiliate programs. When set, that user gets credit when the code is used — the org can track how many sales each influencer drives by joining `Orders.DiscountCodeId` to `DiscountCodes.ReferrerUserId`. This replaces KCGameOn's old `Influencer` and `InfluencerTransaction` tables with a simpler model. Org-wide promo codes (like a "SUMMER2026" campaign) leave `ReferrerUserId` NULL.

### Ticket Dependencies

The `EventTicketDependencies` join table lists prerequisites for a ticket. The `RequireAllDependencies` flag on `EventTickets` controls whether the attendee needs **any one** prerequisite (OR mode, e.g., Food Add-On needs GA or VIP) or **all** prerequisites (AND mode, e.g., a special bundle that requires both a base ticket and a workshop pass). Dependencies are always validated against the **attendee**, not the purchaser — this matters for gift purchases where the buyer may not hold any tickets themselves.

### Currency in Cents

All monetary values are stored as integers in cents to avoid floating-point precision issues. `PriceCents = 2500` means $25.00.

---

## QR Check-In Flow

1. Attendee presents QR code (encodes `TicketCode`)
2. Staff member scans via authenticated check-in web page
3. `POST /orgs/{orgSlug}/events/{eventSlug}/checkin` with `TicketCode` in body
4. API verifies the event's check-in window is open (`CheckinStartsAt <= SYSDATETIMEOFFSET() <= CheckinEndsAt`)
5. API verifies the staff member's role has `CanCheckin = true` on the organization
6. API looks up `UserTickets` by `TicketCode` and `EventId`
7. If `status = 'valid'`: update to `checkedIn`, set `CheckedInAt` and `CheckedInByStaffId`, return success with attendee display info (public metadata like username)
8. If `status = 'checkedIn'`: return warning with original check-in timestamp
9. If `status = 'pendingClaim'`: return error indicating the ticket hasn't been claimed yet, with the masked claim email and purchaser name
10. If not found or `cancelled`: return error

---

## Stripe Connect Payment Flow

1. Organization completes Stripe Connect onboarding, `StripeConnectAccountId` is stored
2. Attendee selects tickets and clicks checkout
3. Platform validates availability, dependencies, and discount codes
4. Inside a serializable transaction with pessimistic locking (`UPDLOCK, HOLDLOCK`), platform re-checks ticket capacity and discount code usage limits, increments `TimesUsed`, creates an Order (status `pending`) and OrderItems, then creates a Stripe Checkout Session using the org's connected account with a platform fee (`application_fee_amount`)
5. Attendee is redirected to Stripe's hosted checkout page
6. On success, Stripe redirects back to the platform's confirmation page
7. Webhook (`checkout.session.completed`) fires → platform updates the Order to `completed`, creates UserTickets (with `valid` or `pendingClaim` status), and sends confirmation emails
8. Funds go directly to the org's Stripe account minus the platform fee

---

## Migration Notes (KCGameOn)

- User accounts are migrated via the `TimbnTicketing.Tools.Migration` console app (`export` from MySQL, `import` to SQL Server)
- Only users with at least one order in `payTable` are migrated
- The old `useraccount.Username` is stored as a `legacyUsername` metadata value; `username` metadata is left empty for users to set on first login
- Users are created with `AuthProviderId = "kcgo-migrated-{id}"` as a placeholder; `UserResolverMiddleware` links them to a real Firebase UID on first sign-in by matching email (only for accounts with the `kcgo-migrated-` prefix)
- Each migrated user gets a membership in the KCGameOn org with the default `Member` role (`IsDefault = true`, `Hierarchy = 100`)
- The `MOD` and `ADMIN` flags on `useraccount` map to corresponding roles in the Roles table under the KCGameOn organization, configured separately
- The old `Influencer` and `InfluencerTransaction` tables are replaced by `DiscountCodes` with `ReferrerUserId` set to the influencer's account
- Recurring venues should be created as Venues records during migration so they're available for future events
- A legacy login flow using Firebase custom tokens is planned to support users who don't have access to their original email (see `docs/kcgameon-integration.md`)
