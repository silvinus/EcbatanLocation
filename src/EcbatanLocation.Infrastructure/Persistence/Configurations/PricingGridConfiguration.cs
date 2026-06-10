using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Infrastructure.Persistence.Configurations;

public class PricingGridConfiguration : IEntityTypeConfiguration<PricingGrid>
{
    public void Configure(EntityTypeBuilder<PricingGrid> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Year).IsRequired();
        builder.HasIndex(g => g.Year).IsUnique();

        builder.HasMany(g => g.Lines)
            .WithOne()
            .HasForeignKey(l => l.PricingGridId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PricingLineConfiguration : IEntityTypeConfiguration<PricingLine>
{
    public void Configure(EntityTypeBuilder<PricingLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ClientType).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(l => l.PricePerDayPerPerson).HasPrecision(10, 2);
    }
}
