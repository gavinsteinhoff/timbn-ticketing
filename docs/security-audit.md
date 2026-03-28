# Security Audit — 2026-03-28

## Fixed

### 1. Ticket overselling race condition (CRITICAL)
**File:** `CheckoutService.cs`
**Fix:** Wrapped capacity check + order creation in a serializable transaction with `UPDLOCK, HOLDLOCK` on OrderItems rows. Concurrent requests now block until the first completes.

### 3. Discount code `TimesUsed` never incremented (CRITICAL)
**File:** `CheckoutService.cs`
**Fix:** After locking the discount code row, `TimesUsed` is now incremented inside the transaction before order creation.

### 6. Discount code `UserId` field was ambiguous (HIGH)
**File:** `DiscountCode.cs`, `DiscountCodeConfiguration.cs`, `User.cs`
**Fix:** Renamed `UserId`/`User` to `ReferrerUserId`/`ReferrerUser` to clarify this tracks who gets credit for the code being used (affiliate/referrer), not a usage restriction. Navigation property on User renamed from `OwnedDiscountCodes` to `ReferredDiscountCodes`.

### 7. Discount code race condition (HIGH)
**File:** `CheckoutService.cs`
**Fix:** Discount code row is locked with `UPDLOCK, HOLDLOCK` inside the serializable transaction. `TimesUsed` is re-checked after locking. Two concurrent requests can't both use the last redemption.

### 16. Integer overflow risk in price calculation (MEDIUM)
**File:** `CheckoutService.cs`
**Fix:** Discount percent validated to 0-100 range, discount cents validated non-negative. Per-item price clamped to `Math.Max(0, priceCents)` after discount application.

---

## Open Issues (documented, not yet fixed)

### CRITICAL

#### 2. Stripe webhook has no signature verification
**File:** `OrderEndpoints.cs:26`
**Issue:** Webhook endpoint is `.AllowAnonymous()` and unimplemented. When built, must verify `Stripe-Signature` header using the webhook signing secret. Without this, anyone can forge payment events.
**Action:** Implement with `Stripe.EventUtility.ConstructEvent()` signature verification.

#### 4. Claim token endpoint leaks info without auth
**File:** `TicketClaimEndpoints.cs:7`
**Issue:** `GET /{claimToken}` requires no auth. If tokens are predictable, anyone can enumerate unclaimed tickets. Token generation strength is undefined.
**Action:** Use 32+ byte cryptographically random tokens. Consider requiring auth on GET. Validate `ClaimExpiresAt` before returning data. Validate claimer email matches `ClaimEmail`.

### HIGH

#### 5. No price validation on ticket creation
**File:** `CreateEventTicketRequest.cs`
**Issue:** `PriceCents` can be negative, `MaxQuantity` can be 0 or negative. No server-side validation.
**Action:** Add validation: `PriceCents >= 0`, `MaxQuantity > 0` if set.

#### 8. Attendee assignment has no consent check
**File:** `CheckoutService.cs`
**Issue:** Any member can assign tickets to any `AttendeeUserId` without the attendee's knowledge or consent.
**Action:** Consider limiting to self-purchase or requiring attendee to be in the same org. This may be acceptable for the gifting use case — discuss with stakeholders.

#### 9. Discount codes visible to all members
**File:** `DiscountCodeEndpoints.cs:10`
**Issue:** GET endpoint has no permission check. Any org member can list all discount codes.
**Action:** Add `.RequirePermission(Permission.CanManageEvents)` to the GET endpoint.

#### 10. Email-based migration linking trusts unverified email
**File:** `UserResolverMiddleware.cs:60-78`
**Issue:** Firebase doesn't guarantee email verification by default. An attacker could register with a victim's email and get linked to their migrated account.
**Action:** Check `email_verified` claim from Firebase JWT before allowing migration linking. Only link if `email_verified == true`.

### MEDIUM

#### 11. Member search LIKE pattern injection
**File:** `MemberSearchService.cs:14`
**Issue:** User input with `%` or `_` characters broadens LIKE pattern matching beyond intent.
**Action:** Escape special characters: `query.Replace("%", "[%]").Replace("_", "[_]")`

#### 12. Member search enables email enumeration
**File:** `MemberSearchService.cs:19-21`
**Issue:** Search matches on email even though email isn't returned. Attacker can confirm email existence by searching and checking result count.
**Action:** Remove email from the search criteria. Search only on name and public metadata.

#### 13. Event privacy flags not enforced
**File:** `EventEndpoints.cs`
**Issue:** Events have `IsPrivate` and `IsPublished` flags but GET endpoints don't filter by them.
**Action:** Add filtering: only return published events on list, enforce privacy on detail endpoint.

#### 14. No max length on search query
**File:** `MemberEndpoints.cs:36`
**Issue:** Min 2 chars but no max. Very long queries could cause slow DB operations.
**Action:** Add max length check (e.g., 100 chars).

#### 15. Migration tool takes connection string via CLI args
**File:** `Tools.Migration/Program.cs`
**Issue:** Visible in shell history and process listings.
**Action:** Support environment variable `KCGO_CONNECTION_STRING` as alternative to CLI arg.

### LOW

- No audit logging on auth failures or permission denials
- No rate limiting on search/enumeration endpoints
- `RoleHierarchy` loaded but never enforced on role mutations
- String-based status fields instead of enums (typo risk)
- Metadata returned on `/me` without filtering by `IsPublic`
