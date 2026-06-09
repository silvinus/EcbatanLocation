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
            dates.Property(d => d.DateDebut).HasColumnName("DateDebut").IsRequired();
            dates.Property(d => d.DateFin).HasColumnName("DateFin").IsRequired();
        });

        builder.Property(r => r.NomLocataire).IsRequired().HasMaxLength(200);
        builder.Property(r => r.NbAdultes).IsRequired();
        builder.Property(r => r.NbEnfantsMoins3Ans).IsRequired();
        builder.Property(r => r.TypeClient).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Statut).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AccepteePar).HasMaxLength(200);
        builder.Property(r => r.ConfirmeePar).HasMaxLength(200);

        builder.HasIndex(r => r.StudioId);
        builder.HasIndex(r => new { r.StudioId, r.Statut });
    }
}
