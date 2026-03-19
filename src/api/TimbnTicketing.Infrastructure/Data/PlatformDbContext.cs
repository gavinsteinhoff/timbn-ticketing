using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data;

public class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();
    public DbSet<UserOrganizationMetadataInfo> UserOrganizationMetadataInfo => Set<UserOrganizationMetadataInfo>();
    public DbSet<UserOrganizationMetadataValue> UserOrganizationMetadataValues => Set<UserOrganizationMetadataValue>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<EventTicket> EventTickets => Set<EventTicket>();
    public DbSet<EventTicketDependency> EventTicketDependencies => Set<EventTicketDependency>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<UserTicket> UserTickets => Set<UserTicket>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
