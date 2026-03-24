using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Api.Services;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Organizations");

        group.MapPost("/", HandleCreateOrganization);

        group.MapGet("/{orgSlug}", HandleGetOrganization)
            .AllowAnonymous()
            .WithName("GetOrganization")
            .WithSummary("Get an organization by slug")
            .Produces<OrganizationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{orgSlug}", HandleUpdateOrganization)
            .RequirePermission(Permission.CanManageOrganization);

        group.MapPost("/{orgSlug}/stripe-connect", HandleInitiateStripeConnect)
            .RequirePermission(Permission.CanManageBilling);

        group.MapGet("/{orgSlug}/stripe-connect/status", HandleGetStripeConnectStatus)
            .RequirePermission(Permission.CanManageBilling);

        return group;
    }

    private static Task<IResult> HandleCreateOrganization() => throw new NotImplementedException();

    private static async Task<IResult> HandleGetOrganization(string orgSlug, OrganizationService organizationService, CurrentRequestContext requestContext)
    {
        if (!requestContext.CanViewOrg)
            return Results.NotFound();

        var org = await organizationService.GetBySlugAsync(orgSlug);

        return org is not null
            ? Results.Ok(org)
            : Results.NotFound();
    }

    private static Task<IResult> HandleUpdateOrganization(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleInitiateStripeConnect(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleGetStripeConnectStatus(string orgSlug) => throw new NotImplementedException();
}
