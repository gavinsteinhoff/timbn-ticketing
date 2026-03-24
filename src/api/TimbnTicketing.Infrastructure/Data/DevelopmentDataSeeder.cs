using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimbnTicketing.Core;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data;

public class DevelopmentDataSeeder : IHostedService
{
    private static readonly Guid _orgId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid _byocTypeId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000001");
    private static readonly Guid _gaTypeId = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000001");
    private static readonly Guid _foodTypeId = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000001");
    private static readonly Guid _eventId = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000001");
    private static readonly Guid _byocEventTicketId = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000001");
    private static readonly Guid _gaEventTicketId = Guid.Parse("a1b2c3d4-0007-0000-0000-000000000001");
    private static readonly Guid _foodEventTicketId = Guid.Parse("a1b2c3d4-0008-0000-0000-000000000001");
    private static readonly Guid _userId = Guid.Parse("a1b2c3d4-0009-0000-0000-000000000001");
    private static readonly Guid _ownerRoleId = Guid.Parse("a1b2c3d4-000a-0000-0000-000000000001");

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
            StripeConnectAccountId = "acct_1TEGxfDt2LBZjiTd"
        };

        var byocType = new TicketType
        {
            Id = _byocTypeId,
            OrganizationId = _orgId,
            Name = "BYOC",
        };

        var gaType = new TicketType
        {
            Id = _gaTypeId,
            OrganizationId = _orgId,
            Name = "GA",
        };

        var foodType = new TicketType
        {
            Id = _foodTypeId,
            OrganizationId = _orgId,
            Name = "FOOD",
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
            StripeProductId = "prod_UCkBS5Lii6Tujg",
            StripePriceId = "price_1TEKhKDt2LBZjiTdCxnj0NZQ"
        };

        var gaTicket = new EventTicket
        {
            Id = _gaEventTicketId,
            EventId = _eventId,
            TicketTypeId = _gaTypeId,
            PriceCents = 0
        };

        var foodTicket = new EventTicket
        {
            Id = _foodEventTicketId,
            EventId = _eventId,
            TicketTypeId = _foodTypeId,
            PriceCents = 1000,
            Dependencies = new List<EventTicketDependency>
            {
                new EventTicketDependency
                {
                    EventTicketId = _foodEventTicketId,
                    RequiresEventTicketId = _gaEventTicketId
                }
            }
        };

        var user = new User
        {
            Id = _userId,
            AuthProviderId = "SdiOndx3wCaKXfHCUOWoSd4zxU12",
            Email = "gavinsteinhoff@gmail.com",
            FirstName = "Gavin",
            LastName = "Steinhoff",
        };

        var ownerRole = new Role
        {
            Id = _ownerRoleId,
            OrganizationId = _orgId,
            Name = "Owner",
            Slug = "owner",
            Hierarchy = 0,
            Permissions = Permission.CanManageOrganization
                | Permission.CanCreateEvents
                | Permission.CanManageEvents
                | Permission.CanManageRoles
                | Permission.CanManageBilling
                | Permission.CanCheckin
                | Permission.CanViewAttendees,
        };

        var userOrg = new UserOrganization
        {
            UserId = _userId,
            OrganizationId = _orgId,
            RoleId = _ownerRoleId,
        };

        db.Organizations.Add(org);
        db.Users.Add(user);
        db.Roles.Add(ownerRole);
        db.UserOrganizations.Add(userOrg);
        db.TicketTypes.AddRange(byocType, gaType, foodType);
        db.Events.Add(evt);
        db.EventTickets.AddRange(byocTicket, gaTicket, foodTicket);

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
