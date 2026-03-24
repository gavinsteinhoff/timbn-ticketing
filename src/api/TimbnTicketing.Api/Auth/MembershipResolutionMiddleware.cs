using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Core;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Auth;

public class MembershipResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformDbContext db, CurrentRequestContext requestContext)
    {
        if (requestContext.IsOrgScoped && requestContext.IsAuthenticated)
        {
            var membership = await db.UserOrganizations
                .Where(uo => uo.UserId == requestContext.UserId && uo.OrganizationId == requestContext.OrganizationId)
                .Select(uo => new
                {
                    uo.RoleId,
                    uo.Role.Hierarchy,
                    uo.Role.Permissions,
                })
                .FirstOrDefaultAsync();

            if (membership is not null)
            {
                requestContext.RoleId = membership.RoleId;
                requestContext.RoleHierarchy = membership.Hierarchy;
                requestContext.GrantPermissions(membership.Permissions);
            }
        }

        await next(context);
    }
}
