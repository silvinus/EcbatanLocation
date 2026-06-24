using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Infrastructure.Persistence.Configurations;

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
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AcceptedBy).HasMaxLength(200);
        builder.Property(r => r.ConfirmedBy).HasMaxLength(200);

        builder.OwnsMany(r => r.PersonLines, pl =>
        {
            pl.ToTable("ReservationPersonLines");
            pl.WithOwner().HasForeignKey("ReservationId");
            pl.Property<int>("Id").ValueGeneratedOnAdd();
            pl.HasKey("Id");
            pl.Property(p => p.ClientType).IsRequired().HasConversion<string>().HasMaxLength(50);
            pl.Property(p => p.AdultCount).IsRequired();
            pl.Property(p => p.ChildrenUnder3Count).IsRequired();
            pl.Ignore(p => p.TotalPersons);
        });

        builder.Property(r => r.ParentReservationId).IsRequired(false);
        builder.HasOne<Reservation>().WithMany().HasForeignKey(r => r.ParentReservationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ParentReservationId);

        builder.Ignore(r => r.TotalPersonCount);
        builder.Ignore(r => r.TotalAdultCount);
        builder.Ignore(r => r.TotalChildrenUnder3Count);
        builder.Ignore(r => r.HasParent);
        builder.Ignore(r => r.DomainEvents);

        builder.HasIndex(r => r.StudioId);
        builder.HasIndex(r => new { r.StudioId, r.Status });
    }
}
