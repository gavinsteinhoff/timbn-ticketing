using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Services;

public class OrganizationService(PlatformDbContext db)
{
    public async Task<OrganizationResponse?> GetBySlugAsync(string slug)
    {
        return await db.Organizations
            .Where(o => o.Slug == slug)
            .Select(o => new OrganizationResponse(o.Id, o.Name, o.Slug, o.LogoUrl, o.WebsiteUrl))
            .FirstOrDefaultAsync();
    }
}
