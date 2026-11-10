namespace HospitalAppointment.Models;

/// <summary>A weekly recurring window in which a doctor accepts appointments.</summary>
public class AvailabilitySlot
{
    public DayOfWeek Day { get; init; }
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }

    public AvailabilitySlot() { }

    public AvailabilitySlot(DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            throw new ArgumentException("Bitiş saati başlangıçtan sonra olmalı.");
        Day = day;
        Start = start;
        End = end;
    }

    public bool Overlaps(AvailabilitySlot other) =>
        Day == other.Day && Start < other.End && other.Start < End;

    /// <summary>True if the whole [start, end) interval on that date fits inside this slot.</summary>
    public bool Contains(DateTime start, DateTime end) =>
        start.DayOfWeek == Day && start.Date == end.Date &&
        TimeOnly.FromDateTime(start) >= Start && TimeOnly.FromDateTime(end) <= End;

    public override string ToString() => $"{Day} {Start:HH:mm}-{End:HH:mm}";
}
