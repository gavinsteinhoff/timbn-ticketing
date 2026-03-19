using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class DiscountCodeEndpoints
{
    public static RouteGroupBuilder MapDiscountCodeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListDiscountCodes);
        group.MapPost("/", HandleCreateDiscountCode)
            .RequirePermission(Permission.CanManageEvents);
        group.MapPatch("/{discountCodeId:guid}", HandleUpdateDiscountCode)
            .RequirePermission(Permission.CanManageEvents);
        group.MapDelete("/{discountCodeId:guid}", HandleDeleteDiscountCode)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListDiscountCodes(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateDiscountCode(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateDiscountCode(string orgSlug, Guid discountCodeId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteDiscountCode(string orgSlug, Guid discountCodeId) => throw new NotImplementedException();
}
