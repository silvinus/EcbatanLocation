using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Infrastructure.Persistence.Configurations;

public class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Nom).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Capacite).IsRequired();
        builder.Property(s => s.ACuisine).IsRequired();
        builder.Property(s => s.LouableSeul).IsRequired();
        builder.Property(s => s.OrdreAffichage).IsRequired();
    }
}
