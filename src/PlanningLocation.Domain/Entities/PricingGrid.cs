using PlanningLocation.Domain.Enums;

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
}
