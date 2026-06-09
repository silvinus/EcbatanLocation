namespace PlanningLocation.Domain.ValueObjects;

public sealed record DateRange
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public DateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public int NumberOfDays => EndDate.DayNumber - StartDate.DayNumber;

    public bool Overlaps(DateRange other)
        => StartDate < other.EndDate && other.StartDate < EndDate;

    public bool ContainsDay(DateOnly day)
        => day >= StartDate && day < EndDate;
}
