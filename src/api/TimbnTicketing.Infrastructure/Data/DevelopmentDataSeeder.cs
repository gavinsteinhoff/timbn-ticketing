using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data;

public class DevelopmentDataSeeder : IHostedService
{
    private static readonly Guid _orgId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid _byocTypeId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000001");
    private static readonly Guid _eventId = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000001");
    private static readonly Guid _byocEventTicketId = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000001");

    private readonly IServiceProvider _serviceProvider;

    public DevelopmentDataSeeder(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        if (await db.Organizations.AnyAsync(o => o.Id == _orgId, cancellationToken))
            return;

        var org = new Organization
        {
            Id = _orgId,
            Name = "KCGameOn",
            Slug = "kcgameon",
            IsPublic = true,
        };

        var byocType = new TicketType
        {
            Id = _byocTypeId,
            OrganizationId = _orgId,
            Name = "BYOC",
        };

        var evt = new Event
        {
            Id = _eventId,
            OrganizationId = _orgId,
            Name = "KCGameOn26",
            Slug = "kcgameon26",
            StartsAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
            IsPublished = true,
            IsPrivate = false,
        };

        var byocTicket = new EventTicket
        {
            Id = _byocEventTicketId,
            EventId = _eventId,
            TicketTypeId = _byocTypeId,
            PriceCents = 5000,
            RequireAllDependencies = false,
        };

        db.Organizations.Add(org);
        db.TicketTypes.AddRange(byocType, byocType);
        db.Events.Add(evt);
        db.EventTickets.AddRange(byocTicket, byocTicket);

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
