namespace PlanningLocation.Domain.ValueObjects;

public sealed record DateRange
{
    public DateOnly DateDebut { get; }
    public DateOnly DateFin { get; }

    public DateRange(DateOnly dateDebut, DateOnly dateFin)
    {
        if (dateFin <= dateDebut)
            throw new ArgumentException("La date de fin doit être postérieure à la date de début.");

        DateDebut = dateDebut;
        DateFin = dateFin;
    }

    public int NombreDeJours => DateFin.DayNumber - DateDebut.DayNumber;

    public bool Chevauche(DateRange other)
        => DateDebut < other.DateFin && other.DateDebut < DateFin;

    public bool ContientJour(DateOnly jour)
        => jour >= DateDebut && jour < DateFin;
}
