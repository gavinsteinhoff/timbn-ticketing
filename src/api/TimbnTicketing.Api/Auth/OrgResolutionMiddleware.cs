using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Auth;

public class OrgResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformDbContext db, CurrentRequestContext requestContext)
    {
        var orgSlug = context.GetRouteValue("orgSlug")?.ToString();

        if (!string.IsNullOrEmpty(orgSlug))
        {
            var org = await db.Organizations
                .Where(o => o.Slug == orgSlug)
                .Select(o => new { o.Id, o.IsPublic })
                .FirstOrDefaultAsync();

            if (org is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = new { code = "ORG_NOT_FOUND" } });
                return;
            }

            requestContext.OrganizationId = org.Id;
            requestContext.IsOrgPublic = org.IsPublic;
        }

        await next(context);
    }
}
