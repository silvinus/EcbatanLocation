using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Domain.ValueObjects;

public class PersonLine
{
    public ClientType ClientType { get; private set; }
    public int AdultCount { get; private set; }
    public int ChildrenUnder3Count { get; private set; }
    public int TotalPersons => AdultCount + ChildrenUnder3Count;

    private PersonLine() { }

    public PersonLine(ClientType clientType, int adultCount, int childrenUnder3Count)
    {
        ClientType = clientType;
        AdultCount = adultCount;
        ChildrenUnder3Count = childrenUnder3Count;
    }
}
