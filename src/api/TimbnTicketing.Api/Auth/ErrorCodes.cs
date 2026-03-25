namespace TimbnTicketing.Api.Auth;

public static class ErrorCodes
{
    public const string OrgNotFound = "ORG_NOT_FOUND";
    public const string EventNotFound = "EVENT_NOT_FOUND";
    public const string NotAMember = "NOT_A_MEMBER";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
}

public static class HttpContextErrorExtensions
{
    public static async Task WriteErrorAsync(this HttpContext context, int statusCode, string errorCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = new { code = errorCode } });
    }
}
