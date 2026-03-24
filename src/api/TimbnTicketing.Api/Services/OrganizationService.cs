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
            .Select(o => new OrganizationResponse(
                o.Id,
                o.Name,
                o.Slug,
                o.LogoUrl,
                o.WebsiteUrl,
                o.Events
                    .Where(e => e.IsPublished && !e.IsPrivate)
                    .OrderByDescending(e => e.StartsAt)
                    .Take(3)
                    .Select(e => new EventSummaryResponse(
                        e.Id,
                        e.Name,
                        e.Slug,
                        e.ShortDescription,
                        e.StartsAt,
                        e.EndsAt,
                        e.IsPublished,
                        e.IsPrivate,
                        e.BannerUrl,
                        e.AvatarUrl,
                        e.Venue != null
                            ? new VenueSummaryResponse(
                                e.Venue.Id,
                                e.Venue.Name,
                                e.Venue.Address,
                                e.Venue.City,
                                e.Venue.State)
                            : null))
                    .ToList()))
            .FirstOrDefaultAsync();
    }
}
