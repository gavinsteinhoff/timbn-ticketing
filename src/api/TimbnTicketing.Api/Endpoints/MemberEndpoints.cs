using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class MemberEndpoints
{
    public static RouteGroupBuilder MapMemberEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListMembers);
        group.MapGet("/search", HandleSearchMembers);
        group.MapPatch("/{userId:guid}/role", HandleUpdateMemberRole)
            .RequirePermission(Permission.CanManageRoles);
        group.MapDelete("/{userId:guid}", HandleRemoveMember)
            .RequirePermission(Permission.CanManageOrganization);
        group.MapGet("/{userId:guid}/metadata", HandleGetMemberMetadata);
        group.MapPut("/{userId:guid}/metadata", HandleSetMemberMetadata)
            .RequirePermission(Permission.CanManageOrganization);

        return group;
    }

    private static Task<IResult> HandleListMembers(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleSearchMembers(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateMemberRole(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleRemoveMember(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleGetMemberMetadata(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleSetMemberMetadata(string orgSlug, Guid userId) => throw new NotImplementedException();
}
