using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Infrastructure.Identity;

namespace EcbatanLocation.Infrastructure.Persistence;

public class EcbatanLocationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PricingGrid> PricingGrids => Set<PricingGrid>();
    public DbSet<PricingLine> PricingLines => Set<PricingLine>();

    public EcbatanLocationDbContext(DbContextOptions<EcbatanLocationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(EcbatanLocationDbContext).Assembly);
    }
}
