namespace TimbnTicketing.Api.Endpoints;

public static class TicketClaimEndpoints
{
    public static RouteGroupBuilder MapTicketClaimEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{claimToken}", HandleGetClaimInfo);
        group.MapPost("/{claimToken}", HandleClaimTicket);
        group.MapPost("/{claimToken}/resend", HandleResendClaimEmail);

        return group;
    }

    private static Task<IResult> HandleGetClaimInfo(string claimToken) => throw new NotImplementedException();
    private static Task<IResult> HandleClaimTicket(string claimToken) => throw new NotImplementedException();
    private static Task<IResult> HandleResendClaimEmail(string claimToken) => throw new NotImplementedException();
}
