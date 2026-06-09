using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Infrastructure.Persistence.Configurations;

public class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.HasKitchen).IsRequired();
        builder.Property(s => s.RentableAlone).IsRequired();
        builder.Property(s => s.DisplayOrder).IsRequired();
    }
}
