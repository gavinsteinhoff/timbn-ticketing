using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class RoleEndpoints
{
    public static RouteGroupBuilder MapRoleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListRoles);
        group.MapPost("/", HandleCreateRole)
            .RequirePermission(Permission.CanManageRoles);
        group.MapPatch("/{roleId:guid}", HandleUpdateRole)
            .RequirePermission(Permission.CanManageRoles);
        group.MapDelete("/{roleId:guid}", HandleDeleteRole)
            .RequirePermission(Permission.CanManageRoles);

        return group;
    }

    private static Task<IResult> HandleListRoles(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateRole(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateRole(string orgSlug, Guid roleId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteRole(string orgSlug, Guid roleId) => throw new NotImplementedException();
}
