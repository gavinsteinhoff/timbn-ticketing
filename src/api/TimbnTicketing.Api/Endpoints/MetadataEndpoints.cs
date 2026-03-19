using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class MetadataEndpoints
{
    public static RouteGroupBuilder MapMetadataEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListMetadataFields);
        group.MapPost("/", HandleCreateMetadataField)
            .RequirePermission(Permission.CanManageOrganization);
        group.MapPatch("/{metadataId:guid}", HandleUpdateMetadataField)
            .RequirePermission(Permission.CanManageOrganization);
        group.MapDelete("/{metadataId:guid}", HandleDeleteMetadataField)
            .RequirePermission(Permission.CanManageOrganization);

        return group;
    }

    private static Task<IResult> HandleListMetadataFields(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateMetadataField(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateMetadataField(string orgSlug, Guid metadataId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteMetadataField(string orgSlug, Guid metadataId) => throw new NotImplementedException();
}
