using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Infrastructure.Persistence.Configurations;

public class ProprietaireConfiguration : IEntityTypeConfiguration<Proprietaire>
{
    public void Configure(EntityTypeBuilder<Proprietaire> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nom).IsRequired().HasMaxLength(100);
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
