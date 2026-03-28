# KCGameOn Integration Guide

This document describes how KCGameOn's website integrates with the Timbn ticketing platform. It is intended to be shared with the KCGameOn repo so that development there has full context on the relationship between the two systems.

## Overview

KCGameOn is a gaming convention in Kansas City (~300+ attendees, ~10+ events/year). They are the first client of the Timbn ticketing platform.

KCGameOn has an existing custom-built website that is being rebuilt. The migration strategy is **ticketing first** — swap out the ticket purchase flow to use Timbn while the rest of the old site stays live. The full site rebuild happens incrementally after that.

## Architecture

```text
┌──────────────────────────────────┐
│   kcgameon.com                   │
│   (existing site, then Next.js)  │
│   - Event info & schedules       │
│   - Ticket selection UI          │
│   - User search for gifting      │
│   - Venue/partner pages          │
└────────┬─────────────────────────┘
         │
         │ Authenticated API calls
         │ (Firebase JWT from Timbn sign-in)
         ▼
┌──────────────────────────────────┐
│   Timbn API                      │
│   - Public event/ticket data     │
│   - Checkout (order + Stripe)    │
│   - User search (metadata)       │
└──────────────────────────────────┘
         │
         │ Stripe Checkout Session
         ▼
┌──────────────────────────────────┐
│   Stripe Checkout                │
│   - Payment only                 │
│   - Redirects back to KCGameOn   │
└──────────────────────────────────┘
```

## Migration Strategy: Ticketing First

### Phase 1 — Swap ticket purchasing (current priority)

KCGameOn's existing site stays live. The only change is replacing the PayPal checkout with Timbn's checkout API + Stripe.

1. Bulk import KCGameOn users into Timbn (see User Migration below)
2. KCGameOn's ticket page keeps its existing UI (ticket selection, quantity, user assignment)
3. On "Purchase", KCGameOn POSTs to Timbn's checkout API with the selected items and attendees
4. Timbn validates, creates the order, returns a Stripe checkout URL
5. KCGameOn redirects the user to Stripe for payment
6. Stripe redirects back to KCGameOn on success/cancel

### Phase 2 — Full site rebuild

After ticketing is stable on Timbn, rebuild the rest of the site (Next.js). Event data, schedules, and ticket listings come from the Timbn API. KCGameOn-specific features (volunteers, partners, gallery) stay on KCGameOn's site.

## Ticket Purchase Flow (Detailed)

KCGameOn's current ticket page has:

- **Ticket types** with descriptions, remaining count, price, and +/- quantity selectors
- **Ticket dependencies** (e.g. "Snacks/Dinner is an ADDON for the GA ticket — cannot be purchased by itself")
- **User assignment dropdown** — search by username/gamer tag to assign tickets to other users
- **PayPal checkout** (being replaced with Stripe via Timbn)

### What KCGameOn handles

- Displaying ticket types, descriptions, and quantities (fetched from Timbn's public API)
- The ticket selection UI (quantity pickers, dependency warnings)
- User search for assigning tickets to other attendees (searches Timbn's user/metadata API)
- Rendering the checkout button and redirecting to Stripe

### What Timbn handles

- Ticket type definitions, pricing, capacity, and dependency rules
- Checkout validation (availability, sales window, dependencies, discount codes)
- Order creation and Stripe Checkout Session creation
- Post-payment processing (order status updates, ticket creation, confirmation emails, QR codes)
- User accounts and profile data (including gaming handles via custom metadata)

### API calls during checkout

```text
1. GET  /orgs/kcgameon/events/{eventSlug}/tickets     → ticket types, prices, remaining
2. GET  /orgs/kcgameon/members?search={query}          → user search for attendee assignment
3. POST /orgs/kcgameon/events/{eventSlug}/orders/checkout
   Body:
   {
     "items": [
       { "eventTicketId": "uuid", "attendeeUserId": "uuid" },
       { "eventTicketId": "uuid", "attendeeEmail": "friend@example.com" }
     ],
     "discountCode": "SUMMER20"
   }
   Response:
   {
     "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_...",
     "sessionId": "cs_...",
     "expiresAt": "2026-06-01T12:30:00Z"
   }
4. Frontend redirects to checkoutUrl
5. Stripe redirects back to KCGameOn success/cancel page
```

### Attendee assignment

Each checkout item has either an `attendeeUserId` (known Timbn user) or an `attendeeEmail` (for people without accounts):

- **Known user** (selected via user search): send their Timbn `userId`. Ticket is assigned directly.
- **Email only** (unknown user): send `attendeeEmail`. Timbn creates a `pendingClaim` ticket and emails the recipient a claim link.
- **Default**: the signed-in user is the attendee (purchaser buys for themselves).

### Ticket dependencies

Timbn's `EventTicketDependency` system already supports KCGameOn's add-on pattern:

- Snacks/Dinner add-on requires GA ticket (OR logic — attendee must hold or be purchasing a GA ticket in the same cart)
- Dependencies are validated server-side during checkout
- KCGameOn's frontend should disable add-on quantity pickers if the prerequisite isn't in the cart (UX hint, not enforced — Timbn validates server-side)

## User Migration

### KCGameOn's current user schema

| Field | Maps to |
|---|---|
| ID | Not migrated (KCGameOn internal ID) |
| Username | Timbn custom metadata: `username` |
| Password | Stored separately for legacy login flow (see Legacy Login section). Not stored in User entity. |
| FirstName | `Users.FirstName` |
| LastName | `Users.LastName` |
| Email | `Users.Email` |
| MOD, ADMIN | Timbn roles/permissions (configured separately) |
| Submission_Date | `Users.CreatedAt` |
| SecretQuestion, SecretAnswer | Not migrated |
| Active | Skip inactive users or flag them |
| DiscordAccount | Timbn custom metadata: `discordAccount` |
| Discord_ID | Timbn custom metadata: `discordId` |
| SteamHandle | Timbn custom metadata: `steamHandle` |
| BattleHandle | Timbn custom metadata: `battleHandle` |
| OriginHandle | Timbn custom metadata: `originHandle` |
| TwitterHandle | Timbn custom metadata: `twitterHandle` |
| PSN_ID | Timbn custom metadata: `psnId` |
| XB_ID | Timbn custom metadata: `xbId` |
| Switch_ID | Timbn custom metadata: `switchId` |
| tshirtSize | Timbn custom metadata: `tshirtSize` |
| Location | Timbn custom metadata: `location` |
| Waiver | Not migrated (event-specific) |
| Tokens | Not migrated (KCGameOn-specific feature) |
| Cerner | Not migrated |

### Migration steps

1. **Define custom metadata fields** on the KCGameOn org in Timbn (`UserOrganizationMetadataInfo` table) — one row per field (username, discordAccount, steamHandle, etc.)
2. **Bulk import users** into Timbn's `Users` table (Email, FirstName, LastName, CreatedAt)
3. **Import metadata values** into `UserOrganizationMetadataValues` for each user's gaming handles
4. **Create org memberships** (`UserOrganizations`) to associate each user with the KCGameOn org with a default attendee role
5. **Firebase accounts are created lazily** — when a migrated user signs in through Timbn for the first time, they'll go through Firebase's sign-up flow. Timbn matches them to their existing record by email.

### User search after migration

KCGameOn's ticket page has a user search dropdown (search by username/gamer tag). After migration, this searches Timbn's API instead of KCGameOn's database. The search endpoint would query Timbn's `Users` + `UserOrganizationMetadataValues` to match on username, name, or email.

## KCGameOn Site Structure (Phase 2 rebuild)

Recommended tech stack: **Next.js** (App Router, TypeScript, Tailwind CSS) for SSR/SEO on public event pages.

| Page | Data Source | Auth? |
|---|---|---|
| `/` | Static + Timbn API (next event) | No |
| `/events` | Timbn API (event list) | No |
| `/events/[slug]` | Timbn API (event detail, schedule, tickets) | No |
| `/events/[slug]/tickets` | Timbn API (ticket types) + checkout flow | Yes (for purchasing) |
| `/about` | Static | No |
| `/volunteer` | Google Form embed | No |
| `/partners` | Static / CMS | No |
| `/gallery` | Static or social media embeds | No |

## KCGameOn-Specific Features

These features live on KCGameOn's site, not in Timbn:

- **Volunteer signup** — currently a Google Form, could become a Timbn platform feature later
- **Partner/sponsor pages** — static content, KCGameOn-specific branding
- **Gallery** — photos/videos from past events
- **Event schedules** — detailed time slots, tournaments, activities (candidate for future Timbn platform feature; static on KCGameOn's site for now)
- **Tokens** — KCGameOn loyalty/reward system (KCGameOn-specific)

## Environment Configuration

```env
TIMBN_API_URL=https://api.timbn.com
TIMBN_ORG_SLUG=kcgameon
```

## Legacy Login (Planned)

Migrated KCGameOn users may not have access to their old email or know which email they used. To support them, a legacy login flow using Firebase custom tokens is planned.

### Flow

1. User enters their KCGameOn **username + password** on the site
2. API validates against the old KCGameOn password hash
3. If valid, generates a Firebase custom token using the Firebase Admin SDK
4. Returns the custom token to the frontend
5. Frontend calls `signInWithCustomToken(token)` → Firebase session established
6. User is prompted to set up a real email/password or link a Google account
7. Legacy login is eventually sunsetted once users have migrated

### Password Hashing (KCGameOn)

KCGameOn uses PBKDF2 (`Rfc2898DeriveBytes`) with a deterministic salt:

```csharp
// Salt is derived from the username using a fixed salt string
var hasher = new Rfc2898DeriveBytes(username,
    Encoding.Default.GetBytes("KcGaM30n"), 10000);
var salt = Convert.ToBase64String(hasher.GetBytes(25));

// Password is hashed using the derived salt
var hasher2 = new Rfc2898DeriveBytes(password,
    Encoding.Default.GetBytes(salt), 10000);
var hash = Convert.ToBase64String(hasher2.GetBytes(25));

// Compare hash to stored value in useraccount.Password
```

### Requirements

- Migration tool must export password hashes (currently excluded)
- Store hashes in Timbn as a metadata field or separate migration table (not in the User entity — these are temporary)
- Firebase Admin SDK dependency for custom token generation
- `POST /auth/legacy-login` endpoint: accepts username + password, returns Firebase custom token
- Frontend integration: `signInWithCustomToken()` call

### References

- [Firebase Custom Auth](https://firebase.google.com/docs/auth/web/custom-auth)
- [Firebase Admin Create Custom Tokens](https://firebase.google.com/docs/auth/admin/create-custom-tokens)

## Current State of Timbn API

As of 2026-03-28:

- The Timbn API exists with endpoints for orgs, events, ticket types, orders, and checkout.
- The checkout endpoint (`POST /orgs/{orgSlug}/events/{eventSlug}/orders/checkout`) creates a Stripe Checkout Session and returns a checkout URL. It validates ticket availability, dependencies, and discount codes.
- **Public anonymous access** to event and ticket listing endpoints is not yet implemented — those routes currently require authentication. This needs to be added before KCGameOn's site can fetch event data without auth.
- **User search endpoint** for querying users by username/metadata does not exist yet. Needs to be built for the attendee assignment dropdown.
- **Timbn's React frontend** is not yet built. For Phase 1, KCGameOn's existing site handles the UI and calls the Timbn API directly.
- CORS configuration for client domains is not yet in place.
- Custom metadata fields (for gaming handles) need to be defined for the KCGameOn org.

## Database

```sql
-- kcgameon.AdminProperties definition

CREATE TABLE `AdminProperties` (
  `BlockPayments` varchar(100) NOT NULL DEFAULT 'TRUE'
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.Charity definition

CREATE TABLE `Charity` (
  `idCharity` int NOT NULL AUTO_INCREMENT,
  `CharityName` varchar(50) NOT NULL,
  `Label` varchar(50) NOT NULL,
  `Description` varchar(500) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  PRIMARY KEY (`idCharity`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.DiscountCodes definition

CREATE TABLE `DiscountCodes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EventId` int DEFAULT NULL,
  `EventTicketId` int DEFAULT NULL,
  `Code` varchar(100) NOT NULL,
  `MoneyDiscount` int NOT NULL DEFAULT '0',
  `PercentDiscount` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- kcgameon.EventArchive definition

CREATE TABLE `EventArchive` (
  `Key` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) NOT NULL,
  `eventID` int NOT NULL,
  `checkedin` tinyint NOT NULL,
  `prize` varchar(50) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  `wondoor` tinyint NOT NULL,
  `wonloyalty` tinyint NOT NULL,
  PRIMARY KEY (`Key`),
  UNIQUE KEY `Key_UNIQUE` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=11094 DEFAULT CHARSET=latin1;


-- kcgameon.EventArchive_copy definition

CREATE TABLE `EventArchive_copy` (
  `Key` int NOT NULL,
  `Username` varchar(50) NOT NULL,
  `eventID` int NOT NULL,
  `checkedin` tinyint NOT NULL,
  `prize` varchar(50) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  `wondoor` tinyint NOT NULL,
  `wonloyalty` tinyint NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.EventSchedule definition

CREATE TABLE `EventSchedule` (
  `IdEventsTable` int NOT NULL,
  `eventID` int NOT NULL DEFAULT '0',
  `item` varchar(75) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `description` varchar(200) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `starttime` datetime NOT NULL,
  `platform` varchar(50) DEFAULT '',
  `URL` varchar(200) DEFAULT NULL,
  `itemAvatar` varchar(100) DEFAULT '' COMMENT 'optional spot to add 25px x 25px avatar for a game',
  `label` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.EventSeats definition

CREATE TABLE `EventSeats` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(255) DEFAULT NULL,
  `across_percent` float NOT NULL,
  `down_percent` float NOT NULL,
  `seat_type` varchar(50) NOT NULL DEFAULT 'BYOC',
  `event_id` int NOT NULL,
  `active` tinyint(1) NOT NULL DEFAULT '1',
  `paytable_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `EventMapSeats_event_id_seat_type_index` (`event_id`,`seat_type`)
) ENGINE=InnoDB AUTO_INCREMENT=2695 DEFAULT CHARSET=latin1 COMMENT='Seats for a given event, on the map';


-- kcgameon.EventSponsors definition

CREATE TABLE `EventSponsors` (
  `IdEventsTable` int DEFAULT NULL,
  `tier` varchar(10) NOT NULL,
  `order` varchar(100) DEFAULT NULL,
  `sponsor` varchar(50) DEFAULT NULL,
  `sponsorURL` varchar(50) DEFAULT NULL,
  `sponsorIMG` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT COMMENT='Dynamically putting sponsors in emails';


-- kcgameon.EventTickets definition

CREATE TABLE `EventTickets` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `eventID` int NOT NULL,
  `TicketType` varchar(50) NOT NULL,
  `Dependency` varchar(10) DEFAULT NULL,
  `numberOfTickets` tinyint NOT NULL DEFAULT '1' COMMENT 'not sure what thie field does now',
  `Label` varchar(50) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL COMMENT 'this is what the ticket should be labled in payTable.',
  `Cost` smallint NOT NULL,
  `Cost2` smallint NOT NULL,
  `Cost3` smallint NOT NULL,
  `playerCost` smallint NOT NULL,
  `Description` varchar(1500) NOT NULL,
  `emailType` varchar(50) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL COMMENT 'this is the emailtype in mailclient.cs',
  `maxqty` int NOT NULL DEFAULT '0' COMMENT 'this field shows in the tickets page',
  `active` tinyint(1) NOT NULL DEFAULT '0',
  `waitlist` varchar(1000) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT '',
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=80 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.Events definition

CREATE TABLE `Events` (
  `IdEventsTable` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL COMMENT 'name of the event',
  `physvirt` varchar(10) NOT NULL COMMENT 'is this a physical or virtual event?',
  `feature` varchar(200) NOT NULL COMMENT 'short description of what the event is, ie. knockout city, gaming convention, 21 and up only',
  `description` varchar(8000) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL COMMENT 'long description of event in html',
  `start` datetime NOT NULL,
  `stop` datetime DEFAULT NULL,
  `eventURL` varchar(200) NOT NULL COMMENT 'place html page is being created',
  `venue` varchar(100) DEFAULT NULL COMMENT 'name of venue',
  `address` varchar(100) DEFAULT NULL COMMENT 'include full address',
  `discordURL` varchar(50) DEFAULT NULL,
  `contact` varchar(100) NOT NULL COMMENT 'person who should be contacted with questions',
  `avatarURL` varchar(100) NOT NULL COMMENT 'place avatar is uploaded to',
  `bannerURL` varchar(100) NOT NULL COMMENT 'place banner is uploaded to',
  `mapURL` varchar(200) NOT NULL COMMENT 'place map file exists for this event',
  `rules` varchar(2500) NOT NULL COMMENT 'rules for each event in html',
  `rulesURL` varchar(50) DEFAULT NULL COMMENT 'future use?',
  `ticketURL` varchar(200) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL COMMENT 'if null, use internal tickets, if contains url, ticket button will go to 3d party site.',
  `ticketDescription` varchar(1000) DEFAULT NULL COMMENT 'sub heading to be used on the ticket page.',
  `kccec` int NOT NULL DEFAULT '0' COMMENT 'active = 1, not active - 0',
  `attendance` int NOT NULL COMMENT 'attendance for the event, to be filled after archive happens',
  `currentEvent` int NOT NULL COMMENT 'active = 1, not active - 0',
  `draft` int NOT NULL COMMENT 'yes =1, no = 0',
  `active` int NOT NULL DEFAULT '0' COMMENT 'active = 1, not active - 0',
  `showMap` int NOT NULL DEFAULT '0' COMMENT 'show map = 1, don''t show map = 0',
  `skipWaiver` int NOT NULL DEFAULT '0' COMMENT 'skip the waiver = 1, don''t skip the waiver = 0',
  `mapSeatSizePercent` float DEFAULT '0' COMMENT 'How many percentage points wide and tall the seats will be on the map',
  `allowCheckin` tinyint(1) DEFAULT '0',
  `hasCharity` bit(1) NOT NULL DEFAULT b'0',
  `charityUrl` varchar(200) DEFAULT NULL,
  `venueURL` varchar(200) DEFAULT NULL,
  `isPrivate` int NOT NULL DEFAULT '0',
  `isBooth` int NOT NULL DEFAULT '0',
  `needWaiver` int NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdEventsTable`)
) ENGINE=InnoDB AUTO_INCREMENT=1022 DEFAULT CHARSET=latin1 COMMENT='For all future events titles, descriptions and more\r\n';


-- kcgameon.RentalTicket definition

CREATE TABLE `RentalTicket` (
  `id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `Firstname` varchar(100) NOT NULL,
  `Lastname` varchar(100) NOT NULL,
  `Email` varchar(50) NOT NULL,
  `RentalLabel` varchar(100) NOT NULL,
  `verifiedPaid` varchar(5) NOT NULL,
  `Submission_Date` datetime NOT NULL,
  `EventID` varchar(25) NOT NULL,
  `activeIndicator` varchar(10) NOT NULL DEFAULT 'TRUE',
  `paymentKey` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.RentalType definition

CREATE TABLE `RentalType` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `RentalHardware` varchar(50) NOT NULL,
  `Label` varchar(50) NOT NULL,
  `Description` varchar(1000) NOT NULL,
  `Cost` tinyint NOT NULL,
  `Capacity` varchar(10) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL DEFAULT 'FALSE',
  `emailType` varchar(50) NOT NULL,
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.TicketTable definition

CREATE TABLE `TicketTable` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `TicketType` varchar(50) NOT NULL,
  `numberOfTickets` tinyint NOT NULL DEFAULT '1',
  `Label` varchar(50) NOT NULL,
  `Cost` smallint NOT NULL,
  `Cost2` smallint NOT NULL,
  `Cost3` smallint NOT NULL,
  `playerCost` smallint NOT NULL,
  `Description` varchar(1500) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  `emailType` varchar(50) NOT NULL,
  `Order` tinyint DEFAULT NULL,
  `maxqty` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=latin1 COMMENT='no longer used.';


-- kcgameon.Token definition

CREATE TABLE `Token` (
  `type` varchar(50) NOT NULL,
  `cost` int NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='never completed development';


-- kcgameon.TournamentTicket definition

CREATE TABLE `TournamentTicket` (
  `id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `TournamentLabel` varchar(15) NOT NULL,
  `Organization` varchar(50) DEFAULT NULL,
  `orderId` varchar(50) NOT NULL,
  `Purchase_Date` datetime NOT NULL,
  `EventID` varchar(25) NOT NULL,
  `activeIndicator` varchar(10) NOT NULL DEFAULT 'TRUE',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.UserActivation definition

CREATE TABLE `UserActivation` (
  `UserID` int NOT NULL,
  `ActivationCode` varchar(500) NOT NULL,
  `ExpiresAtUtc` datetime NOT NULL,
  `CreatedAtUtc` datetime NOT NULL,
  `ActivationCodeType` int NOT NULL,
  PRIMARY KEY (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='Use for code sent to user upon account creation to validate their email.';


-- kcgameon.VendorCompany definition

CREATE TABLE `VendorCompany` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `Company` varchar(50) NOT NULL,
  `Title` varchar(50) NOT NULL,
  `Firstname` varchar(50) NOT NULL,
  `Lastname` varchar(50) NOT NULL,
  `Address` varchar(100) NOT NULL,
  `City` varchar(100) NOT NULL,
  `State` varchar(100) NOT NULL,
  `Zip` varchar(25) NOT NULL,
  `Email` varchar(100) NOT NULL,
  `PhoneNumber` varchar(20) NOT NULL,
  `Website` varchar(50) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.VendorPurchase definition

CREATE TABLE `VendorPurchase` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `TicketType` varchar(50) NOT NULL,
  `Company` varchar(50) NOT NULL,
  `Cost` smallint NOT NULL,
  `paymentType` varchar(50) NOT NULL,
  `paymentKey` varchar(100) NOT NULL,
  `paidDate` datetime NOT NULL,
  `Validation` varchar(25) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.VendorTicket definition

CREATE TABLE `VendorTicket` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `TicketType` varchar(50) NOT NULL,
  `Label` varchar(50) NOT NULL,
  `Cost` smallint NOT NULL,
  `Cost2` smallint NOT NULL,
  `Cost3` smallint NOT NULL,
  `Description` text NOT NULL,
  `activeIndicator` varchar(50) NOT NULL,
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.VolunteerTicket definition

CREATE TABLE `VolunteerTicket` (
  `id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `Firstname` varchar(100) NOT NULL,
  `Lastname` varchar(100) NOT NULL,
  `Email` varchar(50) NOT NULL,
  `VolunteerType` varchar(100) NOT NULL,
  `Submission_Date` datetime NOT NULL,
  `EventID` varchar(25) NOT NULL,
  `activeIndicator` varchar(10) NOT NULL DEFAULT 'TRUE',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=687 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.VolunteerType definition

CREATE TABLE `VolunteerType` (
  `idTicketTable` int NOT NULL AUTO_INCREMENT,
  `VolunteerType` varchar(50) NOT NULL,
  `Label` varchar(50) NOT NULL,
  `Description` varchar(1000) NOT NULL,
  `Capacity` varchar(10) NOT NULL,
  `activeIndicator` varchar(50) NOT NULL DEFAULT 'FALSE',
  `emailType` varchar(50) NOT NULL,
  `Priority` tinyint DEFAULT NULL,
  `EventId` int NOT NULL DEFAULT '92',
  PRIMARY KEY (`idTicketTable`)
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.Waiver definition

CREATE TABLE `Waiver` (
  `tableID` int NOT NULL AUTO_INCREMENT,
  `username` varchar(100) NOT NULL,
  `signedName` varchar(100) NOT NULL,
  `TT` varchar(10) DEFAULT NULL,
  `datetime` datetime NOT NULL,
  `isMinor` tinyint(1) DEFAULT '0',
  `eventId` int NOT NULL,
  `wondoor` varchar(50) NOT NULL DEFAULT '0' COMMENT '0 = available to win, 1= won, 2= epic fail',
  `wonloyalty` varchar(50) NOT NULL DEFAULT '0',
  `active` int NOT NULL DEFAULT '1' COMMENT '0 = nonactive, player dropped from tournament.',
  `wintime` datetime DEFAULT NULL,
  PRIMARY KEY (`tableID`)
) ENGINE=InnoDB AUTO_INCREMENT=905 DEFAULT CHARSET=latin1 COMMENT='to track who and when the waivers were signed.  This should be done each year.';


-- kcgameon.WaiverTable definition

CREATE TABLE `WaiverTable` (
  `eventID` int NOT NULL,
  `waiverText` varchar(4000) NOT NULL,
  `minorText` varchar(4000) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.__EFMigrationsHistory definition

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- kcgameon.`archivedseating (not used)` definition

CREATE TABLE `archivedseating (not used)` (
  `eventID` int NOT NULL,
  `seatID` int NOT NULL,
  `username` varchar(100) DEFAULT NULL,
  KEY `seatID` (`seatID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.checkIn definition

CREATE TABLE `checkIn` (
  `Key` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) NOT NULL,
  `eventID` int NOT NULL,
  `checkedin_time` datetime NOT NULL,
  `wondoor` varchar(50) NOT NULL,
  `wonloyalty` varchar(50) NOT NULL,
  `active` int DEFAULT '1' COMMENT '0 = nonactive, player dropped from tournament.',
  `signedName` varchar(50) DEFAULT NULL,
  `isMinor` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`Key`),
  UNIQUE KEY `Key_UNIQUE` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=3043 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT COMMENT='called by https://www.kcgameon.com/checkin';


-- kcgameon.checkIntest definition

CREATE TABLE `checkIntest` (
  `Key` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) NOT NULL,
  `eventID` int NOT NULL,
  `checkedin_time` datetime NOT NULL,
  `wondoor` varchar(50) NOT NULL,
  `wonloyalty` varchar(50) NOT NULL,
  PRIMARY KEY (`Key`),
  UNIQUE KEY `Key_UNIQUE` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT COMMENT='was used in testing - isn''t an active table.';


-- kcgameon.mailingList definition

CREATE TABLE `mailingList` (
  `username` varchar(50) NOT NULL COMMENT 'pull username from useraccount',
  `email` varchar(100) NOT NULL COMMENT 'pull email from useracount',
  `activation` varchar(50) NOT NULL COMMENT 'what they clicked on to activate the opt in',
  `optinDate` datetime NOT NULL COMMENT 'what datetime they opted in',
  `optoutDate` datetime DEFAULT NULL COMMENT 'what datetime they opted out',
  `active` int NOT NULL DEFAULT '0' COMMENT '0 = opted out, 1 = opted in'
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='opt-in style mailing list.';


-- kcgameon.orders definition

CREATE TABLE `orders` (
  `order_id` varchar(50) NOT NULL,
  `status` varchar(50) NOT NULL,
  `payer_name` varchar(100) NOT NULL,
  `payer_id` varchar(50) NOT NULL,
  `payer_email` varchar(50) NOT NULL,
  `purchase_reference_id` varchar(50) NOT NULL,
  `purchase_address` varchar(100) NOT NULL,
  `item_count` varchar(15) NOT NULL,
  `address1` varchar(100) NOT NULL,
  `address2` varchar(100) DEFAULT NULL,
  `city` varchar(50) NOT NULL,
  `state` varchar(15) NOT NULL,
  `zip` varchar(10) NOT NULL,
  `payment_id` varchar(25) NOT NULL,
  `payment_status` varchar(25) NOT NULL,
  `gross_amount` varchar(10) NOT NULL,
  `paypal_fee` varchar(10) NOT NULL,
  `net_amount` varchar(10) NOT NULL,
  `seller_protection_status` varchar(25) NOT NULL,
  `seller_protection_dispute_categories` varchar(50) NOT NULL,
  `final_capture` varchar(20) NOT NULL,
  `disbursement_mode` varchar(20) NOT NULL,
  `create_time` datetime NOT NULL,
  `update_time` datetime NOT NULL,
  `receipt` varchar(1000) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='Order table for all things going to paypal starting 2020.';


-- kcgameon.payTable definition

CREATE TABLE `payTable` (
  `idpayTable` int NOT NULL AUTO_INCREMENT,
  `userName` varchar(100) NOT NULL,
  `verifiedPaid` varchar(3) NOT NULL,
  `TT` varchar(25) NOT NULL,
  `Charity` varchar(10) NOT NULL DEFAULT '0',
  `donationAmount` double DEFAULT '0',
  `paymentMethod` varchar(45) NOT NULL,
  `paymentKey` varchar(100) DEFAULT NULL,
  `paidDate` datetime NOT NULL,
  `eventID` int NOT NULL,
  `activeIndicator` varchar(45) NOT NULL DEFAULT 'FALSE',
  `Barcode` bigint DEFAULT NULL,
  `paidSat` int NOT NULL DEFAULT '0',
  `paidSun` int NOT NULL DEFAULT '0',
  `tournament` varchar(45) DEFAULT NULL,
  `ReferralCode` varchar(100) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL,
  `DiscountCode` varchar(100) DEFAULT NULL,
  `UserId` varchar(36) DEFAULT NULL,
  `EventTicketId` varchar(36) DEFAULT NULL,
  `DatabaseVersion` int DEFAULT NULL,
  `PurchaserUserId` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`idpayTable`),
  KEY `userName_idx` (`userName`)
) ENGINE=InnoDB AUTO_INCREMENT=10917 DEFAULT CHARSET=latin1;


-- kcgameon.prizes definition

CREATE TABLE `prizes` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `Prize` varchar(50) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL,
  `Picture` varchar(50) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT '0',
  `Event` int NOT NULL DEFAULT '72',
  `ClaimedBy` varchar(50) DEFAULT NULL,
  `Status` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=latin1;


-- kcgameon.retired_tournaments definition

CREATE TABLE `retired_tournaments` (
  `id` int DEFAULT NULL,
  `username` varchar(50) DEFAULT NULL,
  `EventID` varchar(50) DEFAULT NULL,
  `SFV` tinyint(1) DEFAULT '0',
  `KingOfFighters` tinyint(1) DEFAULT '0',
  `GuiltyGear` tinyint(1) DEFAULT '0',
  `KI` tinyint(1) DEFAULT '0',
  `Skullgirls` tinyint(1) DEFAULT '0',
  `UltraSF4` tinyint(1) DEFAULT '0',
  `BlazeBlue` tinyint(1) DEFAULT '0',
  `SF3` tinyint(1) DEFAULT '0',
  `MKX` tinyint(1) DEFAULT '0',
  `MVC` tinyint(1) DEFAULT '0',
  `DOA5` tinyint(1) DEFAULT '0',
  `Pokken` tinyint(1) DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.schedule definition

CREATE TABLE `schedule` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `Tournament` varchar(100) NOT NULL,
  `TournamentDate` varchar(100) DEFAULT NULL,
  `EventStart` datetime NOT NULL,
  `EventStop` datetime NOT NULL,
  `Active` int NOT NULL,
  `Attendance` int NOT NULL DEFAULT '0',
  `EventID` int NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=70 DEFAULT CHARSET=latin1;


-- kcgameon.seatcoords definition

CREATE TABLE `seatcoords` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(45) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `floor` varchar(50) DEFAULT NULL,
  `seattype` varchar(45) DEFAULT NULL,
  `comments` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2504 DEFAULT CHARSET=latin1 COMMENT='no longer used.';


-- kcgameon.`seatcoords_1.15.16` definition

CREATE TABLE `seatcoords_1.15.16` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(45) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `user` varchar(45) DEFAULT NULL,
  `comments` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=223 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.`seatcoords_11.8.15` definition

CREATE TABLE `seatcoords_11.8.15` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(45) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `user` varchar(45) DEFAULT NULL,
  `comments` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=219 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.`seatcoords_4.25.15` definition

CREATE TABLE `seatcoords_4.25.15` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(45) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `user` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=197 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.seatcoords_test definition

CREATE TABLE `seatcoords_test` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(45) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `floor` int DEFAULT NULL,
  `seattype` varchar(45) DEFAULT NULL,
  `comments` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1957 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.seatingchart definition

CREATE TABLE `seatingchart` (
  `seatID` int NOT NULL,
  `username` varchar(100) DEFAULT NULL,
  `EventID` int NOT NULL,
  `EventDate` date NOT NULL,
  `ActiveIndicator` varchar(45) NOT NULL DEFAULT 'TRUE',
  `checkedin` tinyint(1) DEFAULT '0',
  `checkedin_time` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='no longer used';


-- kcgameon.tournamentCheckIn definition

CREATE TABLE `tournamentCheckIn` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `tournamentID` int NOT NULL,
  `checkInTime` datetime NOT NULL,
  `active` int DEFAULT '1',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `Key_UNIQUE` (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.tournamentGames definition

CREATE TABLE `tournamentGames` (
  `gameID` varchar(50) NOT NULL,
  `IdEventTable` int NOT NULL,
  `gametitle` varchar(50) NOT NULL,
  `gamePoster` varchar(50) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `role1` varchar(50) NOT NULL,
  `role2` varchar(50) NOT NULL,
  `role3` varchar(50) NOT NULL,
  `role4` varchar(50) NOT NULL,
  `role5` varchar(50) NOT NULL,
  `role6` varchar(50) NOT NULL,
  `role7` varchar(50) NOT NULL,
  `role8` varchar(50) NOT NULL,
  `active` int NOT NULL DEFAULT '1',
  `allowTeamSignups` tinyint(1) NOT NULL DEFAULT '1',
  `allowFreeAgentSignups` tinyint(1) NOT NULL DEFAULT '1',
  `RulesDocLink` varchar(500) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.tournamentMaps definition

CREATE TABLE `tournamentMaps` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tournamentID` int NOT NULL,
  `week` int NOT NULL,
  `map` varchar(100) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=197 DEFAULT CHARSET=latin1 COMMENT='To select stages or maps/modes for each game, to be pushed to the tournament page.';


-- kcgameon.tournamentPlayers definition

CREATE TABLE `tournamentPlayers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `gametitle` varchar(50) NOT NULL,
  `teamname` varchar(100) NOT NULL,
  `playername` varchar(50) NOT NULL,
  `firstname` varchar(50) NOT NULL,
  `lastname` varchar(50) NOT NULL,
  `email` varchar(50) NOT NULL,
  `role` varchar(50) DEFAULT NULL,
  `rank` varchar(50) NOT NULL,
  `teamOwner` int NOT NULL DEFAULT '0',
  `teamAdmin` int NOT NULL DEFAULT '0',
  `active` int NOT NULL DEFAULT '1' COMMENT '1= active, 0= retired, -1= dropped out',
  `tournamentTeamId` int NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idTournamentPlayers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2508 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.tournamentRanks definition

CREATE TABLE `tournamentRanks` (
  `gameID` int NOT NULL,
  `rank` varchar(50) NOT NULL,
  `rankOrder` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.tournamentRoles definition

CREATE TABLE `tournamentRoles` (
  `gameID` varchar(50) NOT NULL,
  `role` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.tournamentSponsors definition

CREATE TABLE `tournamentSponsors` (
  `IdEventsTable` int DEFAULT NULL,
  `order` varchar(100) DEFAULT NULL,
  `sponsor` varchar(50) DEFAULT NULL,
  `sponsorURL` varchar(50) DEFAULT NULL,
  `sponsorIMG` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='Dynamically putting sponsors in emails';


-- kcgameon.tournamentTeams definition

CREATE TABLE `tournamentTeams` (
  `id` int NOT NULL AUTO_INCREMENT,
  `gametitle` varchar(50) NOT NULL,
  `teamname` varchar(50) NOT NULL,
  `owner1` varchar(50) NOT NULL,
  `owner1email` varchar(50) NOT NULL,
  `owner2` varchar(50) NOT NULL,
  `owner2email` varchar(50) NOT NULL,
  `owner3` varchar(50) NOT NULL,
  `owner3email` varchar(50) NOT NULL,
  `createddate` datetime NOT NULL,
  `active` int NOT NULL DEFAULT '1',
  `tournamentID` int NOT NULL COMMENT 'tied to ''tournaments'' table',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=576 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT;


-- kcgameon.tournaments definition

CREATE TABLE `tournaments` (
  `IdEventsTable` int NOT NULL COMMENT 'related to eventID from events table - can run mulitple tournaments at one event.',
  `tournamentID` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL COMMENT 'name of the event',
  `feature` varchar(200) NOT NULL COMMENT 'short description of what the event is, ie. knockout city, gaming convention, 21 and up only',
  `description` varchar(1500) NOT NULL COMMENT 'long description of event',
  `start` datetime NOT NULL,
  `stop` datetime DEFAULT NULL,
  `tournamentURL` varchar(200) NOT NULL COMMENT 'place html page is being created',
  `style` varchar(100) DEFAULT NULL COMMENT 'swiss, pool play, single, double elimination',
  `bestof` varchar(100) DEFAULT NULL COMMENT 'include full address',
  `rounds` varchar(100) NOT NULL COMMENT 'how many rounds are used in this tournament',
  `avatarURL` varchar(100) NOT NULL COMMENT 'place avatar is uploaded to',
  `bannerURL` varchar(100) NOT NULL COMMENT 'place banner is uploaded to',
  `kccec` int NOT NULL DEFAULT '0' COMMENT 'active = 1, not active - 0',
  `active` int NOT NULL COMMENT 'active = 1, not active - 0',
  `allowTeamSignups` tinyint(1) NOT NULL,
  `allowFreeAgentSignups` tinyint(1) NOT NULL,
  `streamedGames` tinyint NOT NULL DEFAULT '0',
  `gameId` varchar(50) DEFAULT NULL,
  `RulesDocLink` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`tournamentID`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=39 DEFAULT CHARSET=latin1 ROW_FORMAT=COMPACT COMMENT='For all future events titles, descriptions and more\r\n';


-- kcgameon.useraccount definition

CREATE TABLE `useraccount` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Email` varchar(200) NOT NULL,
  `MOD` int NOT NULL DEFAULT '0',
  `ADMIN` int NOT NULL DEFAULT '0',
  `Submission_Date` datetime DEFAULT CURRENT_TIMESTAMP,
  `SecretQuestion` varchar(500) NOT NULL,
  `SecretAnswer` varchar(500) NOT NULL,
  `Cerner` varchar(50) DEFAULT NULL,
  `Active` int NOT NULL DEFAULT '0',
  `DiscordAccount` varchar(50) NOT NULL,
  `SteamHandle` varchar(60) NOT NULL,
  `BattleHandle` varchar(60) NOT NULL,
  `OriginHandle` varchar(60) NOT NULL,
  `TwitterHandle` varchar(60) NOT NULL,
  `Discord_ID` varchar(50) CHARACTER SET latin1 COLLATE latin1_bin NOT NULL COMMENT 'add discord id',
  `PSN_ID` varchar(60) NOT NULL,
  `XB_ID` varchar(60) NOT NULL,
  `Switch_ID` varchar(60) NOT NULL,
  `Waiver` varchar(1) NOT NULL,
  `tshirtSize` varchar(3) NOT NULL,
  `Location` int unsigned NOT NULL,
  `Tokens` int unsigned NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `Username` (`Username`)
) ENGINE=InnoDB AUTO_INCREMENT=5755 DEFAULT CHARSET=latin1;


-- kcgameon.userinfo definition

CREATE TABLE `userinfo` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `SteamID` varchar(100) NOT NULL,
  `GamingGroup` varchar(100) NOT NULL,
  `GamingInitials` varchar(100) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1;


-- kcgameon.EventRegistration definition

CREATE TABLE `EventRegistration` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `userID` int NOT NULL,
  `eventID` int NOT NULL,
  `paid` int DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `ID_idx` (`userID`),
  KEY `eventID_idx` (`eventID`),
  CONSTRAINT `eventID` FOREIGN KEY (`eventID`) REFERENCES `schedule` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `userID` FOREIGN KEY (`userID`) REFERENCES `useraccount` (`ID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- kcgameon.Influencer definition

CREATE TABLE `Influencer` (
  `influencerid` int unsigned NOT NULL AUTO_INCREMENT,
  `influencername` varchar(100) NOT NULL,
  `username` varchar(100) NOT NULL,
  `referralcount` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`influencerid`),
  UNIQUE KEY `Influencer_uniqueInfluencerName` (`influencername`),
  UNIQUE KEY `Influencer_uniqueUserName` (`username`),
  CONSTRAINT `Influencer_FK` FOREIGN KEY (`username`) REFERENCES `useraccount` (`Username`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=1013 DEFAULT CHARSET=latin1;


-- kcgameon.InfluencerTransaction definition

CREATE TABLE `InfluencerTransaction` (
  `transactionid` int unsigned NOT NULL AUTO_INCREMENT,
  `influencerid` int unsigned NOT NULL,
  `influencername` varchar(100) NOT NULL,
  `purchaser` varchar(100) NOT NULL,
  `purchasetime` varchar(100) NOT NULL,
  `ticketrecipient` varchar(100) NOT NULL,
  `eventid` varchar(100) NOT NULL,
  PRIMARY KEY (`transactionid`),
  KEY `purchaserFK` (`purchaser`),
  KEY `influenceridFK` (`influencerid`),
  CONSTRAINT `influenceridFK` FOREIGN KEY (`influencerid`) REFERENCES `Influencer` (`influencerid`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `purchaserFK` FOREIGN KEY (`purchaser`) REFERENCES `useraccount` (`Username`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=latin1;


-- kcgameon.tournamentMatch definition

CREATE TABLE `tournamentMatch` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tournamentID` int NOT NULL,
  `round` int DEFAULT NULL,
  `awayUsername` varchar(100) DEFAULT NULL,
  `homeUsername` varchar(100) DEFAULT NULL,
  `awayPoints` int DEFAULT NULL,
  `homePoints` int DEFAULT NULL,
  `awayTotalScore` int DEFAULT NULL,
  `homeTotalScore` int DEFAULT NULL,
  `isStreamed` tinyint(1) NOT NULL DEFAULT '0',
  `bracketNumber` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `FK_tournamentMatch_tournaments` (`tournamentID`),
  CONSTRAINT `FK_tournamentMatch_tournaments` FOREIGN KEY (`tournamentID`) REFERENCES `tournaments` (`tournamentID`)
) ENGINE=InnoDB AUTO_INCREMENT=869 DEFAULT CHARSET=latin1;
```
