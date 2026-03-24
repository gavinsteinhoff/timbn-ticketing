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
            var eventId = await db.Events
                .Where(e => e.Slug == eventSlug && e.OrganizationId == requestContext.OrganizationId)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            if (eventId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = new { code = "EVENT_NOT_FOUND" } });
                return;
            }

            requestContext.EventId = eventId;
        }

        await next(context);
    }
}
