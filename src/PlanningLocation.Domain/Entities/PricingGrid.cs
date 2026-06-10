using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Entities;

public class PricingGrid
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    private List<PricingLine> _lines = [];
    public IReadOnlyCollection<PricingLine> Lines => _lines.AsReadOnly();

    private PricingGrid() { }

    public static PricingGrid Create(int year, IEnumerable<PricingLine> lines)
    {
        return new PricingGrid
        {
            Id = Guid.NewGuid(),
            Year = year,
            _lines = lines.ToList()
        };
    }

    public decimal GetRate(ClientType clientType)
    {
        return _lines.FirstOrDefault(l => l.ClientType == clientType)?.PricePerDayPerPerson
               ?? throw new InvalidOperationException($"No rate defined for {clientType} in {Year}.");
    }

    public void Update(IEnumerable<PricingLine> lines)
    {
        _lines = lines.ToList();
    }

    /// <summary>
    /// Computes the total price for the given person lines over a number of days.
    /// Business rule: children under 3 of "Acquaintance" clients are charged half rate;
    /// for every other client type children are charged the full per-person rate.
    /// </summary>
    public decimal CalculateAmount(IEnumerable<PersonLine> personLines, int numberOfDays)
    {
        var total = 0m;
        foreach (var line in personLines)
        {
            var rate = GetRate(line.ClientType);
            var childRate = line.ClientType == ClientType.Acquaintance ? rate * 0.5m : rate;
            total += (line.AdultCount * rate + line.ChildrenUnder3Count * childRate) * numberOfDays;
        }

        return total;
    }
}
