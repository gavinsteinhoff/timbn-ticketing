using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Auth;

public class EventResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformDbContext db, CurrentRequestContext requestContext)
    {
        var eventSlug = context.GetRouteValue("eventSlug")?.ToString();

        if (!string.IsNullOrEmpty(eventSlug) && requestContext.OrganizationId.HasValue)
        {
            var eventInfo = await db.Events
                .Where(e => e.Slug == eventSlug && e.OrganizationId == requestContext.OrganizationId)
                .Select(e => new { e.Id, e.Name })
                .FirstOrDefaultAsync();

            if (eventInfo is null)
            {
                await context.WriteErrorAsync(StatusCodes.Status404NotFound, ErrorCodes.EventNotFound);
                return;
            }

            requestContext.EventId = eventInfo.Id;
            requestContext.EventName = eventInfo.Name;
        }

        await next(context);
    }
}
