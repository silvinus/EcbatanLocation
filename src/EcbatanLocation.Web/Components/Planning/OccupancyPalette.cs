using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Web.Components.Planning;

// Shared availability color used by the month, week and mobile agenda views so the
// gradient stays consistent everywhere.
public static class OccupancyPalette
{
    // Gradient from red (no place available) to green (everything free), driven by the
    // share of available places. Saturation/lightness tuned for the dark theme.
    public static string Color(DailyOccupationDto occ)
    {
        var ratio = occ.TotalCapacity > 0 ? (double)occ.AvailablePlaces / occ.TotalCapacity : 1.0;
        var hue = (int)Math.Round(ratio * 140); // 0 = red, 140 = green
        return $"hsl({hue}, 65%, 45%)";
    }
}
