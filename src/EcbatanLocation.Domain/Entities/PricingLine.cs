using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Domain.Entities;

public class PricingLine
{
    public Guid Id { get; private set; }
    public Guid PricingGridId { get; private set; }
    public ClientType ClientType { get; private set; }
    public decimal PricePerDayPerPerson { get; private set; }

    private PricingLine() { }

    public static PricingLine Create(ClientType clientType, decimal pricePerDayPerPerson)
    {
        return new PricingLine
        {
            Id = Guid.NewGuid(),
            ClientType = clientType,
            PricePerDayPerPerson = pricePerDayPerPerson
        };
    }
}
