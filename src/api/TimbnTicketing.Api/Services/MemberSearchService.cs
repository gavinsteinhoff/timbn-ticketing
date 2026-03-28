using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Services;

public class MemberSearchService(PlatformDbContext db)
{
    public async Task<MemberSearchResponse> SearchAsync(
        Guid organizationId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = $"%{query}%";

        // Find user IDs that match on name, email, or public metadata values
        var userIdsByName = db.UserOrganizations
            .Where(uo => uo.OrganizationId == organizationId)
            .Where(uo => EF.Functions.Like(uo.User.FirstName, searchTerm)
                || EF.Functions.Like(uo.User.LastName, searchTerm)
                || EF.Functions.Like(uo.User.Email, searchTerm))
            .Select(uo => uo.UserId);

        var userIdsByMetadata = db.UserOrganizationMetadataValues
            .Where(mv => mv.OrganizationId == organizationId
                && mv.MetadataInfo.IsPublic
                && EF.Functions.Like(mv.MetadataValue, searchTerm))
            .Select(mv => mv.UserId);

        var matchingUserIds = await userIdsByName
            .Union(userIdsByMetadata)
            .Distinct()
            .Take(10)
            .ToListAsync(cancellationToken);

        if (matchingUserIds.Count == 0)
            return new MemberSearchResponse([]);

        // Load user info + public metadata for matched users
        var users = await db.Users
            .Where(u => matchingUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
            })
            .ToListAsync(cancellationToken);

        var metadata = await db.UserOrganizationMetadataValues
            .Where(mv => matchingUserIds.Contains(mv.UserId)
                && mv.OrganizationId == organizationId
                && mv.MetadataInfo.IsPublic)
            .Select(mv => new
            {
                mv.UserId,
                mv.MetadataInfo.MetadataName,
                mv.MetadataValue,
            })
            .ToListAsync(cancellationToken);

        var metadataByUser = metadata
            .GroupBy(m => m.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(m => m.MetadataName, m => m.MetadataValue));

        var results = users.Select(u => new MemberSearchResult(
            u.Id,
            u.FirstName,
            u.LastName,
            metadataByUser.GetValueOrDefault(u.Id, [])
        )).ToList();

        return new MemberSearchResponse(results);
    }
}
