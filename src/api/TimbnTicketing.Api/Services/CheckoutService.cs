using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Api.Dtos.Requests;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Core.Entities;
using TimbnTicketing.Core.Interfaces;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Services;

public class OrderCheckoutService(PlatformDbContext db, IStripeCheckoutService stripeOrderCheckoutService)
{
    public async Task<CheckoutResult> CreateCheckoutAsync(
        Guid organizationId,
        Guid eventId,
        Guid purchaserUserId,
        string stripeConnectAccountId,
        CheckoutRequest request,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            return CheckoutResult.Fail("INVALID_REQUEST", "At least one item is required.");

        foreach (var item in request.Items)
        {
            if (item.AttendeeUserId is null && string.IsNullOrWhiteSpace(item.AttendeeEmail))
                return CheckoutResult.Fail("INVALID_REQUEST", "Each item must have either attendeeUserId or attendeeEmail.");

            if (item.AttendeeUserId is not null && !string.IsNullOrWhiteSpace(item.AttendeeEmail))
                return CheckoutResult.Fail("INVALID_REQUEST", "Each item must have either attendeeUserId or attendeeEmail, not both.");
        }

        var now = DateTimeOffset.UtcNow;
        var requestedTicketIds = request.Items.Select(i => i.EventTicketId).Distinct().ToList();

        // Load all referenced event tickets with their dependencies
        var eventTickets = await db.EventTickets
            .Include(et => et.TicketType)
            .Include(et => et.Dependencies)
            .Where(et => et.EventId == eventId && requestedTicketIds.Contains(et.Id))
            .ToListAsync(cancellationToken);

        if (eventTickets.Count != requestedTicketIds.Count)
            return CheckoutResult.Fail("EVENT_TICKET_NOT_FOUND", "One or more ticket types were not found.");

        // Validate each ticket type is active and within sales window
        foreach (var et in eventTickets)
        {
            if (!et.IsActive)
                return CheckoutResult.Fail("TICKET_NOT_ACTIVE", $"Ticket '{et.TicketType.Name}' is not currently active.");

            if (et.SalesStartAt.HasValue && now < et.SalesStartAt.Value)
                return CheckoutResult.Fail("SALES_NOT_STARTED", $"Sales for '{et.TicketType.Name}' have not started yet.");

            if (et.SalesEndAt.HasValue && now > et.SalesEndAt.Value)
                return CheckoutResult.Fail("SALES_ENDED", $"Sales for '{et.TicketType.Name}' have ended.");
        }

        var quantityByTicket = request.Items
            .GroupBy(i => i.EventTicketId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Resolve attendee emails to user IDs where possible
        var attendeeEmails = request.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.AttendeeEmail))
            .Select(i => i.AttendeeEmail!)
            .Distinct()
            .ToList();

        var emailToUser = attendeeEmails.Count > 0
            ? await db.Users
                .Where(u => attendeeEmails.Contains(u.Email))
                .ToDictionaryAsync(u => u.Email, u => u.Id, cancellationToken)
            : new Dictionary<string, Guid>();

        // Validate ticket dependencies
        var allAttendeeUserIds = request.Items
            .Select(i => i.AttendeeUserId ?? emailToUser.GetValueOrDefault(i.AttendeeEmail ?? ""))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingTickets = allAttendeeUserIds.Count > 0
            ? await db.UserTickets
                .Where(ut => ut.EventId == eventId
                    && allAttendeeUserIds.Contains(ut.UserId!.Value)
                    && (ut.Status == "valid" || ut.Status == "checkedIn"))
                .Select(ut => new { ut.UserId, ut.OrderItem.EventTicketId })
                .ToListAsync(cancellationToken)
            : [];

        var existingTicketsByUser = existingTickets
            .GroupBy(t => t.UserId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(t => t.EventTicketId).ToHashSet());

        var cartTicketsByAttendee = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var item in request.Items)
        {
            var attendeeId = item.AttendeeUserId ?? emailToUser.GetValueOrDefault(item.AttendeeEmail ?? "");
            if (attendeeId == Guid.Empty) continue;

            if (!cartTicketsByAttendee.TryGetValue(attendeeId, out var set))
            {
                set = [];
                cartTicketsByAttendee[attendeeId] = set;
            }
            set.Add(item.EventTicketId);
        }

        foreach (var item in request.Items)
        {
            var et = eventTickets.First(t => t.Id == item.EventTicketId);
            if (et.Dependencies.Count == 0) continue;

            var attendeeId = item.AttendeeUserId ?? emailToUser.GetValueOrDefault(item.AttendeeEmail ?? "");
            var heldTicketIds = new HashSet<Guid>();

            if (attendeeId != Guid.Empty)
            {
                if (existingTicketsByUser.TryGetValue(attendeeId, out var existing))
                    heldTicketIds.UnionWith(existing);
                if (cartTicketsByAttendee.TryGetValue(attendeeId, out var cart))
                    heldTicketIds.UnionWith(cart);
            }

            var requiredTicketIds = et.Dependencies.Select(d => d.RequiresEventTicketId).ToList();

            bool satisfied = et.RequireAllDependencies
                ? requiredTicketIds.All(id => heldTicketIds.Contains(id))
                : requiredTicketIds.Any(id => heldTicketIds.Contains(id));

            if (!satisfied)
                return CheckoutResult.Fail("DEPENDENCY_NOT_MET", $"Ticket '{et.TicketType.Name}' requires a prerequisite ticket.");
        }

        // Validate discount code (outside transaction — read-only check first)
        DiscountCode? discountCode = null;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            discountCode = await db.DiscountCodes
                .FirstOrDefaultAsync(dc => dc.Code == request.DiscountCode
                    && dc.OrganizationId == organizationId
                    && dc.IsActive
                    && (!dc.EventId.HasValue || dc.EventId == eventId)
                    && (!dc.ExpiresAt.HasValue || dc.ExpiresAt > now)
                    && (!dc.MaxUses.HasValue || dc.TimesUsed < dc.MaxUses),
                    cancellationToken);

            if (discountCode is null)
                return CheckoutResult.Fail("INVALID_DISCOUNT_CODE", "The discount code is invalid or expired.");


        }

        if (discountCode is not null)
        {
            if (discountCode.DiscountPercent < 0 || discountCode.DiscountPercent > 100)
                return CheckoutResult.Fail("INVALID_DISCOUNT_CODE", "Discount code has an invalid percentage.");
            if (discountCode.DiscountCents < 0)
                return CheckoutResult.Fail("INVALID_DISCOUNT_CODE", "Discount code has an invalid amount.");
        }

        // Calculate totals with price clamping
        var totalCents = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var et = eventTickets.First(t => t.Id == item.EventTicketId);
            var priceCents = et.PriceCents;

            if (discountCode is not null)
            {
                var appliesToTicket = !discountCode.EventTicketId.HasValue || discountCode.EventTicketId == et.Id;
                if (appliesToTicket)
                {
                    if (discountCode.DiscountPercent > 0)
                        priceCents = priceCents - (priceCents * discountCode.DiscountPercent / 100);
                    else if (discountCode.DiscountCents > 0)
                        priceCents -= discountCode.DiscountCents;
                }
            }

            priceCents = Math.Max(0, priceCents);

            totalCents += priceCents;
            orderItems.Add(new OrderItem
            {
                EventTicketId = item.EventTicketId,
                PriceCents = priceCents,
            });
        }

        // Platform fee: 2.5% of total (safe — totalCents is non-negative)
        var platformFeeCents = (int)Math.Ceiling(totalCents * 0.025);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Lock and re-check ticket capacity inside the transaction
            foreach (var et in eventTickets)
            {
                if (!et.MaxQuantity.HasValue) continue;

                var soldCount = await db.OrderItems
                    .FromSqlRaw(
                        """
                        SELECT oi.*
                        FROM OrderItems oi WITH (UPDLOCK, HOLDLOCK)
                        INNER JOIN Orders o ON o.Id = oi.OrderId
                        WHERE oi.EventTicketId = {0}
                        AND o.Status NOT IN ('failed', 'refunded')
                        """,
                        et.Id)
                    .CountAsync(cancellationToken);

                var requested = quantityByTicket[et.Id];

                if (soldCount + requested > et.MaxQuantity.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return CheckoutResult.Fail("SOLD_OUT",
                        $"Not enough '{et.TicketType.Name}' tickets available. {et.MaxQuantity.Value - soldCount} remaining.");
                }
            }

            if (discountCode is not null)
            {
                var lockedCode = await db.DiscountCodes
                    .FromSqlRaw(
                        "SELECT * FROM DiscountCodes WITH (UPDLOCK, HOLDLOCK) WHERE Id = {0}",
                        discountCode.Id)
                    .FirstAsync(cancellationToken);

                if (lockedCode.MaxUses.HasValue && lockedCode.TimesUsed >= lockedCode.MaxUses.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return CheckoutResult.Fail("INVALID_DISCOUNT_CODE", "This discount code has reached its usage limit.");
                }

                lockedCode.TimesUsed++;
            }

            // Create order
            var order = new Order
            {
                UserId = purchaserUserId,
                OrganizationId = organizationId,
                EventId = eventId,
                Status = "pending",
                TotalCents = totalCents,
                PlatformFeeCents = platformFeeCents,
                DiscountCodeId = discountCode?.Id,
                Items = orderItems,
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);

            // Build Stripe line items grouped by price
            var stripeLineItems = orderItems
                .GroupBy(oi => oi.EventTicketId)
                .Select(g =>
                {
                    var et = eventTickets.First(t => t.Id == g.Key);
                    return new StripeCheckoutLineItem(et.StripePriceId!, g.Count());
                })
                .ToList();

            var result = await stripeOrderCheckoutService.CreateCheckoutSessionAsync(
                stripeConnectAccountId,
                order.Id,
                stripeLineItems,
                platformFeeCents,
                successUrl,
                cancelUrl,
                cancellationToken);

            // Store Stripe session ID on the order
            order.StripeCheckoutSessionId = result.SessionId;
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return CheckoutResult.Ok(new CheckoutResponse(result.Url, result.SessionId, result.ExpiresAt));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public class CheckoutResult
{
    public bool IsSuccess { get; private init; }
    public CheckoutResponse? Response { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static CheckoutResult Ok(CheckoutResponse response) => new()
    {
        IsSuccess = true,
        Response = response,
    };

    public static CheckoutResult Fail(string errorCode, string message) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = message,
    };
}
