using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Auth;

public class MembershipEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requestContext = context.HttpContext.RequestServices.GetRequiredService<CurrentRequestContext>();

        if (!requestContext.IsMember)
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
        var requestContext = context.HttpContext.RequestServices.GetRequiredService<CurrentRequestContext>();

        if (!requestContext.IsMember)
        {
            return Results.Json(new { error = new { code = "NOT_A_MEMBER" } }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (!requestContext.HasPermission(permission))
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
