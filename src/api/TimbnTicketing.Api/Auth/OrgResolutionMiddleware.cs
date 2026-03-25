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
                .Select(o => new { o.Id, o.IsPublic, o.StripeConnectAccountId })
                .FirstOrDefaultAsync();

            if (org is null)
            {
                await context.WriteErrorAsync(StatusCodes.Status404NotFound, ErrorCodes.OrgNotFound);
                return;
            }

            requestContext.OrganizationId = org.Id;
            requestContext.IsOrgPublic = org.IsPublic;
            requestContext.OrgStripeConnectAccountId = org.StripeConnectAccountId;
        }

        await next(context);
    }
}
