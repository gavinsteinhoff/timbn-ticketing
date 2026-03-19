namespace TimbnTicketing.Api.Endpoints;

public static class TicketEndpoints
{
    public static RouteGroupBuilder MapTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{ticketCode}/qr", HandleGetQrCode);

        return group;
    }

    private static Task<IResult> HandleGetQrCode(string ticketCode) => throw new NotImplementedException();
}
