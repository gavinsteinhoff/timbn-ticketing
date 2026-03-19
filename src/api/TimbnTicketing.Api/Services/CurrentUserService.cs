using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Services;

public class CurrentUserService(PlatformDbContext db)
{
    public async Task<CurrentUserResponse?> GetByIdAsync(Guid userId)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.CreatedAt,
                Organizations = u.UserOrganizations.Select(uo => new
                {
                    uo.OrganizationId,
                    uo.Organization.Slug,
                    uo.Organization.Name,
                    Role = new { uo.Role.Id, uo.Role.Slug, uo.Role.Name }
                }).ToList(),
                Metadata = u.MetadataValues.Select(mv => new
                {
                    mv.MetadataInfo.OrganizationId,
                    mv.MetadataInfo.MetadataName,
                    mv.MetadataValue
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (user is null) return null;

        var metadataByOrg = user.Metadata
            .GroupBy(m => m.OrganizationId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(m => m.MetadataName, m => m.MetadataValue));

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            user.Organizations.Select(o => new CurrentUserOrganizationResponse(
                o.OrganizationId,
                o.Slug,
                o.Name,
                new CurrentUserRoleResponse(o.Role.Id, o.Role.Slug, o.Role.Name),
                metadataByOrg.GetValueOrDefault(o.OrganizationId, [])
            )).ToList());
    }
}
