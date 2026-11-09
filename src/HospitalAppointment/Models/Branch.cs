namespace HospitalAppointment.Models;

/// <summary>One physical location of an institution, with its own hours, departments and services.</summary>
public class Branch
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public TimeOnly OpensAt { get; set; } = new(8, 0);
    public TimeOnly ClosesAt { get; set; } = new(17, 0);
    public List<DayOfWeek> WorkingDays { get; set; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public List<Department> Departments { get; set; } = [];
    public List<MedicalService> Services { get; set; } = [];

    public MedicalService? FindService(string id) => Services.FirstOrDefault(s => s.Id == id);

    /// <summary>True if the branch is open for the entire [start, end) interval.</summary>
    public bool IsOpen(DateTime start, DateTime end) =>
        WorkingDays.Contains(start.DayOfWeek) && start.Date == end.Date &&
        TimeOnly.FromDateTime(start) >= OpensAt && TimeOnly.FromDateTime(end) <= ClosesAt;

    public override string ToString() => $"{Name} ({OpensAt:HH:mm}-{ClosesAt:HH:mm})";
}
