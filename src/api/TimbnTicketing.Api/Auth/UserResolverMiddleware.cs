using Azure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimbnTicketing.Core.Entities;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Auth;

/// <summary>
/// Resolves the JWT "sub" claim (Firebase UID stored as AuthProviderId) to a platform UserId
/// and populates the scoped CurrentRequestContext for the request.
/// Auto-provisions a User row on first authenticated API call using claims from the JWT.
/// </summary>
public class UserResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformDbContext db, CurrentRequestContext requestContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            await RunAuthLogic(context, db, requestContext);

        await next(context);
    }

    private static async Task RunAuthLogic(HttpContext context, PlatformDbContext db, CurrentRequestContext requestContext)
    {
        var authProviderId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(authProviderId))
            return;

        var userId = await db.Users
            .Where(u => u.AuthProviderId == authProviderId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (userId == Guid.Empty)
        {
            userId = await TryLinkMigratedUserAsync(context, db, authProviderId);
        }

        if (userId == Guid.Empty)
        {
            userId = await AutoProvisionUserAsync(context, db, authProviderId);
        }

        if (userId != Guid.Empty)
        {
            requestContext.UserId = userId;
            requestContext.AuthProviderId = authProviderId;
        }
    }

    private const string MigrationPrefix = "kcgo-migrated-";

    /// <summary>
    /// Handles migrated users: if no AuthProviderId match but an email match exists
    /// AND the existing account has a migration placeholder, link the Firebase UID.
    /// Non-migrated email collisions are ignored — AutoProvision will handle them.
    /// </summary>
    private static async Task<Guid> TryLinkMigratedUserAsync(
        HttpContext context, PlatformDbContext db, string authProviderId)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.FindFirstValue("email");

        if (string.IsNullOrEmpty(email))
            return Guid.Empty;

        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser is null || !existingUser.AuthProviderId.StartsWith(MigrationPrefix))
            return Guid.Empty;

        existingUser.AuthProviderId = authProviderId;
        await db.SaveChangesAsync();

        return existingUser.Id;
    }

    private static async Task<Guid> AutoProvisionUserAsync(
        HttpContext context, PlatformDbContext db, string authProviderId)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.FindFirstValue("email")
            ?? string.Empty;

        var fullName = context.User.FindFirstValue(ClaimTypes.Name)
            ?? context.User.FindFirstValue("name")
            ?? string.Empty;

        var (firstName, lastName) = SplitName(fullName);

        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthProviderId = authProviderId,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
            return user.Id;
        }
        catch (DbUpdateException)
        {
            // Concurrent first request from the same user — re-query by AuthProviderId
            db.Entry(user).State = EntityState.Detached;
            return await db.Users
                .Where(u => u.AuthProviderId == authProviderId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return (string.Empty, string.Empty);

        var spaceIndex = fullName.IndexOf(' ');
        if (spaceIndex < 0)
            return (fullName, string.Empty);

        return (fullName[..spaceIndex], fullName[(spaceIndex + 1)..]);
    }
}
