using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcbatanLocation.Web.Services;

public class ReportPdfGenerator
{
    public byte[] Generate(ReservationReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(30);
                page.MarginVertical(25);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReservationReportDto report)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Ecbatan Location — Rapport de réservations")
                    .FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
                row.ConstantItem(200).AlignRight().Text($"Généré le {report.GeneratedAt:dd/MM/yyyy à HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });

            col.Item().PaddingTop(4).Text(report.PeriodLabel)
                .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingBottom(8);
        });
    }

    private static void ComposeContent(IContainer container, ReservationReportDto report)
    {
        container.Column(col =>
        {
            if (report.Lines.Count == 0)
            {
                col.Item().PaddingVertical(20).AlignCenter()
                    .Text("Aucune réservation pour cette période.").FontSize(12).FontColor(Colors.Grey.Medium);
                return;
            }

            col.Item().Element(c => ComposeTable(c, report.Lines));
            col.Item().PaddingTop(15).Element(c => ComposeSummary(c, report.Summary));
        });
    }

    private static void ComposeTable(IContainer container, IReadOnlyList<ReportLineDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.2f);  // Studio
                cols.RelativeColumn(1.2f);  // Locataire
                cols.RelativeColumn(1f);    // Propriétaire
                cols.RelativeColumn(1.3f);  // Dates
                cols.ConstantColumn(35);    // Jours
                cols.RelativeColumn(2f);    // Détail personnes
                cols.ConstantColumn(60);    // Montant
                cols.ConstantColumn(65);    // Statut
            });

            table.Header(header =>
            {
                var style = TextStyle.Default.FontSize(8).Bold().FontColor(Colors.White);

                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Studio").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Locataire").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Propriétaire").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Dates").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignCenter().Text("Jours").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Détail personnes").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight().Text("Montant").Style(style);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Statut").Style(style);
            });

            var even = false;
            foreach (var line in lines)
            {
                var bg = even ? Colors.Grey.Lighten4 : Colors.White;
                even = !even;

                table.Cell().Background(bg).Padding(3).Text(line.StudioName).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.TenantName).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.OwnerName).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text($"{line.StartDate:dd/MM} → {line.EndDate:dd/MM}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{line.NumberOfDays}").FontSize(8);

                table.Cell().Background(bg).Padding(3).Column(personCol =>
                {
                    foreach (var pl in line.PersonLines)
                    {
                        var detail = $"{pl.ClientTypeLabel}: {pl.AdultCount} ad.";
                        if (pl.ChildrenUnder3Count > 0)
                            detail += $" + {pl.ChildrenUnder3Count} enf.";
                        detail += $" × {pl.RatePerDay:0.00}€ = {pl.LineAmount:0.00}€";
                        personCol.Item().Text(detail).FontSize(7);
                    }
                });

                table.Cell().Background(bg).Padding(3).AlignRight()
                    .Text(line.TotalAmount.HasValue ? $"{line.TotalAmount:0.00} €" : "N/A")
                    .FontSize(8).Bold();

                table.Cell().Background(bg).Padding(3)
                    .Text(GetStatusLabel(line.Status))
                    .FontSize(8).FontColor(GetStatusColor(line.Status));
            }
        });
    }

    private static void ComposeSummary(IContainer container, ReportSummaryDto summary)
    {
        container.Column(outer =>
        {
            outer.Item().Row(row =>
            {
                row.RelativeItem().Text($"Réservations : {summary.TotalReservations}").FontSize(10);
                row.RelativeItem().Text($"Nuitées : {summary.TotalNights}").FontSize(10);
                row.RelativeItem().Text($"Montant total : {summary.TotalAmount:0.00} €").FontSize(10).Bold();
            });

            outer.Item().PaddingTop(10).Text("Totaux par propriétaire et par statut").FontSize(11).Bold();

            outer.Item().PaddingTop(6).Table(table =>
            {
                var statuses = summary.ByStatus.Select(s => s.Status).ToList();
                var colCount = 2 + statuses.Count;

                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.5f);
                    foreach (var _ in statuses)
                        cols.RelativeColumn(1f);
                    cols.RelativeColumn(1f);
                });

                var headerStyle = TextStyle.Default.FontSize(8).Bold().FontColor(Colors.White);
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Propriétaire").Style(headerStyle);
                    foreach (var status in statuses)
                        header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight()
                            .Text(GetStatusLabel(status)).Style(headerStyle);
                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight()
                        .Text("Total").Style(headerStyle);
                });

                var even = false;
                foreach (var owner in summary.ByOwner)
                {
                    var bg = even ? Colors.Grey.Lighten4 : Colors.White;
                    even = !even;

                    table.Cell().Background(bg).Padding(3).Column(c =>
                    {
                        c.Item().Text(owner.OwnerName).FontSize(8).Bold();
                        c.Item().Text($"{owner.Count} rés. / {owner.TotalNights} nuits").FontSize(7).FontColor(Colors.Grey.Medium);
                    });

                    foreach (var status in statuses)
                    {
                        var cell = summary.ByOwnerAndStatus
                            .FirstOrDefault(x => x.OwnerName == owner.OwnerName && x.Status == status);
                        table.Cell().Background(bg).Padding(3).AlignRight().Column(c =>
                        {
                            if (cell is not null)
                            {
                                c.Item().Text($"{cell.TotalAmount:0.00} €").FontSize(8);
                                c.Item().Text($"{cell.Count} rés.").FontSize(7).FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                c.Item().Text("—").FontSize(8).FontColor(Colors.Grey.Lighten1);
                            }
                        });
                    }

                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text($"{owner.TotalAmount:0.00} €").FontSize(8).Bold();
                }

                table.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Total").FontSize(8).Bold();
                foreach (var status in statuses)
                {
                    var s = summary.ByStatus.First(x => x.Status == status);
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Column(c =>
                    {
                        c.Item().Text($"{s.TotalAmount:0.00} €").FontSize(8).Bold();
                        c.Item().Text($"{s.Count} rés.").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                }
                table.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight()
                    .Text($"{summary.TotalAmount:0.00} €").FontSize(8).Bold().FontColor(Colors.Green.Darken2);
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    private static string GetStatusLabel(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "Demande",
        ReservationStatus.Accepted => "Acceptée",
        ReservationStatus.Confirmed => "Confirmée",
        _ => status.ToString()
    };

    private static string GetStatusColor(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "#CC8800",
        ReservationStatus.Accepted => "#4477CC",
        ReservationStatus.Confirmed => "#228855",
        _ => Colors.Black
    };
}
