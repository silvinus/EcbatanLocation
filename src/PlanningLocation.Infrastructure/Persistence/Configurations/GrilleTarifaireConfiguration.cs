using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Infrastructure.Persistence.Configurations;

public class GrilleTarifaireConfiguration : IEntityTypeConfiguration<GrilleTarifaire>
{
    public void Configure(EntityTypeBuilder<GrilleTarifaire> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Annee).IsRequired();
        builder.HasIndex(g => g.Annee).IsUnique();

        builder.HasMany(g => g.Lignes)
            .WithOne()
            .HasForeignKey(l => l.GrilleTarifaireId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LigneTarifConfiguration : IEntityTypeConfiguration<LigneTarif>
{
    public void Configure(EntityTypeBuilder<LigneTarif> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.TypeClient).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(l => l.PrixParJourParPersonne).HasPrecision(10, 2);
    }
}
