using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningLocation.Domain.Entities;

namespace PlanningLocation.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.Dates, dates =>
        {
            dates.Property(d => d.StartDate).HasColumnName("StartDate").IsRequired();
            dates.Property(d => d.EndDate).HasColumnName("EndDate").IsRequired();
        });

        builder.Property(r => r.TenantName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.AdultCount).IsRequired();
        builder.Property(r => r.ChildrenUnder3Count).IsRequired();
        builder.Property(r => r.ClientType).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AcceptedBy).HasMaxLength(200);
        builder.Property(r => r.ConfirmedBy).HasMaxLength(200);

        builder.HasIndex(r => r.StudioId);
        builder.HasIndex(r => new { r.StudioId, r.Status });
    }
}
