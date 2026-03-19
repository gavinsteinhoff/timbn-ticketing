using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Core;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Auth;

public class MembershipResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformDbContext db, CurrentUserContext userContext)
    {
        if (userContext.IsOrgScoped && userContext.IsAuthenticated)
        {
            var membership = await db.UserOrganizations
                .Where(uo => uo.UserId == userContext.UserId && uo.OrganizationId == userContext.OrganizationId)
                .Select(uo => new
                {
                    uo.RoleId,
                    uo.Role.Hierarchy,
                    uo.Role.Permissions,
                })
                .FirstOrDefaultAsync();

            if (membership is not null)
            {
                userContext.RoleId = membership.RoleId;
                userContext.RoleHierarchy = membership.Hierarchy;
                userContext.GrantPermissions(membership.Permissions);
            }
        }

        await next(context);
    }
}
