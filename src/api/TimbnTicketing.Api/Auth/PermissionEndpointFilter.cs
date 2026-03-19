using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Auth;

public class MembershipEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userContext = context.HttpContext.RequestServices.GetRequiredService<CurrentUserContext>();

        if (!userContext.IsMember)
        {
            return Results.Json(new { error = new { code = "NOT_A_MEMBER" } }, statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

public class PermissionEndpointFilter(Permission permission) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userContext = context.HttpContext.RequestServices.GetRequiredService<CurrentUserContext>();

        if (!userContext.IsMember)
        {
            return Results.Json(new { error = new { code = "NOT_A_MEMBER" } }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (!userContext.HasPermission(permission))
        {
            return Results.Json(new { error = new { code = "INSUFFICIENT_PERMISSIONS" } }, statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

public static class PermissionExtensions
{
    public static RouteGroupBuilder RequireMembership(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<MembershipEndpointFilter>();
        return group;
    }

    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, Permission permission)
    {
        builder.AddEndpointFilter(new PermissionEndpointFilter(permission));
        return builder;
    }
}
