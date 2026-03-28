using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Api.Services;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class MemberEndpoints
{
    public static RouteGroupBuilder MapMemberEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Members");

        group.MapGet("/", HandleListMembers);

        group.MapGet("/search", HandleSearchMembers)
            .WithName("SearchMembers")
            .WithSummary("Search for members")
            .WithDescription("Search members by name, email, or public metadata values (e.g. username). Returns up to 10 matches. Does not return email addresses.")
            .Produces<MemberSearchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

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

    private static async Task<IResult> HandleSearchMembers(
        string orgSlug,
        string? q,
        MemberSearchService memberSearchService,
        CurrentRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.Problem(
                detail: "Search query must be at least 2 characters.",
                statusCode: StatusCodes.Status400BadRequest);

        var result = await memberSearchService.SearchAsync(
            requestContext.OrganizationId!.Value,
            q,
            cancellationToken);

        return Results.Ok(result);
    }

    private static Task<IResult> HandleUpdateMemberRole(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleRemoveMember(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleGetMemberMetadata(string orgSlug, Guid userId) => throw new NotImplementedException();
    private static Task<IResult> HandleSetMemberMetadata(string orgSlug, Guid userId) => throw new NotImplementedException();
}
