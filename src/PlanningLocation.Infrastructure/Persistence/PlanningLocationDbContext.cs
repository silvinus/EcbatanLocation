using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Infrastructure.Identity;

namespace PlanningLocation.Infrastructure.Persistence;

public class PlanningLocationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PricingGrid> PricingGrids => Set<PricingGrid>();
    public DbSet<PricingLine> PricingLines => Set<PricingLine>();

    public PlanningLocationDbContext(DbContextOptions<PlanningLocationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PlanningLocationDbContext).Assembly);
    }
}
