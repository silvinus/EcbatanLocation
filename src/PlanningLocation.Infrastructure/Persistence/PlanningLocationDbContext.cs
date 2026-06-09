using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Infrastructure.Identity;

namespace PlanningLocation.Infrastructure.Persistence;

public class PlanningLocationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<Proprietaire> Proprietaires => Set<Proprietaire>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<GrilleTarifaire> GrillesTarifaires => Set<GrilleTarifaire>();
    public DbSet<LigneTarif> LignesTarifs => Set<LigneTarif>();

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
