using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetReservationReport;

public class GetReservationReportQueryHandler(
    IReservationRepository reservationRepository,
    IPricingGridRepository pricingGridRepository,
    IStudioRepository studioRepository,
    IOwnerRepository ownerRepository) : IRequestHandler<GetReservationReportQuery, ReservationReportDto>
{
    public async Task<ReservationReportDto> Handle(GetReservationReportQuery request, CancellationToken cancellationToken)
    {
        var reservations = request.Month.HasValue
            ? await reservationRepository.GetByMonthAsync(request.Year, request.Month.Value, cancellationToken)
            : await reservationRepository.GetByYearAsync(request.Year, cancellationToken);

        var studios = await studioRepository.GetAllAsync(cancellationToken);
        var owners = await ownerRepository.GetAllAsync(cancellationToken);
        var pricingGrid = await pricingGridRepository.GetByYearAsync(request.Year, cancellationToken);

        var studioMap = studios.ToDictionary(s => s.Id, s => s.Name);
        var ownerMap = owners.ToDictionary(o => o.Id, o => o.Name);

        var lines = reservations
            .OrderBy(r => r.Dates.StartDate)
            .ThenBy(r => r.Dates.EndDate)
            .Select(r =>
            {
                var personLines = r.PersonLines.Select(pl =>
                {
                    decimal? rate = null;
                    decimal? lineAmount = null;

                    if (pricingGrid is not null)
                    {
                        rate = pricingGrid.GetRate(pl.ClientType);
                        var childRate = pl.ClientType == ClientType.Acquaintance ? rate.Value * 0.5m : rate.Value;
                        lineAmount = (pl.AdultCount * rate.Value + pl.ChildrenUnder3Count * childRate) * r.Dates.NumberOfDays;
                    }

                    return new ReportPersonLineDto(
                        GetClientTypeLabel(pl.ClientType),
                        pl.AdultCount,
                        pl.ChildrenUnder3Count,
                        rate ?? 0m,
                        lineAmount ?? 0m);
                }).ToList();

                decimal? totalAmount = pricingGrid is not null
                    ? personLines.Sum(pl => pl.LineAmount)
                    : null;

                return new ReportLineDto(
                    r.Id,
                    studioMap.GetValueOrDefault(r.StudioId, "Inconnu"),
                    ownerMap.GetValueOrDefault(r.OwnerId, "Inconnu"),
                    r.TenantName,
                    r.Dates.StartDate,
                    r.Dates.EndDate,
                    r.Dates.NumberOfDays,
                    personLines,
                    totalAmount,
                    r.Status);
            })
            .ToList();

        var summary = BuildSummary(lines, ownerMap, reservations);

        var periodLabel = request.Month.HasValue
            ? $"{GetMonthName(request.Month.Value)} {request.Year}"
            : $"Année {request.Year}";

        return new ReservationReportDto(
            request.Year,
            request.Month,
            periodLabel,
            lines,
            summary,
            DateTime.Now);
    }

    private static ReportSummaryDto BuildSummary(
        List<ReportLineDto> lines,
        Dictionary<Guid, string> ownerMap,
        IReadOnlyList<Domain.Entities.Reservation> reservations)
    {
        var totalReservations = lines.Count;
        var totalNights = lines.Sum(l => l.NumberOfDays);
        var totalAmount = lines.Sum(l => l.TotalAmount ?? 0m);

        var byStatus = lines
            .GroupBy(l => l.Status)
            .Select(g => new ReportStatusSummaryDto(g.Key, g.Count(), g.Sum(l => l.TotalAmount ?? 0m)))
            .OrderBy(s => s.Status)
            .ToList();

        var byOwner = reservations
            .GroupBy(r => r.OwnerId)
            .Select(g =>
            {
                var ownerLines = lines.Where(l => g.Any(r => r.Id == l.ReservationId)).ToList();
                return new ReportOwnerSummaryDto(
                    ownerMap.GetValueOrDefault(g.Key, "Inconnu"),
                    ownerLines.Count,
                    ownerLines.Sum(l => l.NumberOfDays),
                    ownerLines.Sum(l => l.TotalAmount ?? 0m));
            })
            .OrderBy(o => o.OwnerName)
            .ToList();

        var byOwnerAndStatus = reservations
            .GroupBy(r => (r.OwnerId, r.Status))
            .Select(g =>
            {
                var ownerLines = lines.Where(l => g.Any(r => r.Id == l.ReservationId)).ToList();
                return new ReportOwnerStatusSummaryDto(
                    ownerMap.GetValueOrDefault(g.Key.OwnerId, "Inconnu"),
                    g.Key.Status,
                    ownerLines.Count,
                    ownerLines.Sum(l => l.NumberOfDays),
                    ownerLines.Sum(l => l.TotalAmount ?? 0m));
            })
            .OrderBy(o => o.OwnerName)
            .ThenBy(o => o.Status)
            .ToList();

        return new ReportSummaryDto(totalReservations, totalNights, totalAmount, byStatus, byOwner, byOwnerAndStatus);
    }

    private static string GetClientTypeLabel(ClientType type) => type switch
    {
        ClientType.Owner => "Propriétaire",
        ClientType.GuestWithPresence => "Invité avec présence",
        ClientType.Acquaintance => "Connaissance",
        ClientType.MobileHome => "Mobil-home",
        ClientType.Tent => "Tente",
        _ => type.ToString()
    };

    private static string GetMonthName(int month) => month switch
    {
        1 => "Janvier",
        2 => "Février",
        3 => "Mars",
        4 => "Avril",
        5 => "Mai",
        6 => "Juin",
        7 => "Juillet",
        8 => "Août",
        9 => "Septembre",
        10 => "Octobre",
        11 => "Novembre",
        12 => "Décembre",
        _ => month.ToString()
    };
}
