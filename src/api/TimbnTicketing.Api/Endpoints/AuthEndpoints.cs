namespace TimbnTicketing.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/callback", HandleCallback);

        return group;
    }

    private static Task<IResult> HandleCallback() => throw new NotImplementedException();
}
