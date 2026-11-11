using System.Text.Json.Serialization;

namespace HospitalAppointment.Models;

/// <summary>
/// A booked appointment. Service details are copied at booking time so later catalog
/// changes do not alter history. Status only changes through Confirm/Reject/Cancel.
/// </summary>
public class Appointment : IEntity
{
    [JsonConstructor]
    private Appointment() { }

    public static Appointment Create(Patient patient, Doctor doctor, Branch branch, MedicalService service, DateTime start) =>
        new()
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            InstitutionId = doctor.InstitutionId,
            BranchId = branch.Id,
            BranchName = branch.Name,
            ServiceId = service.Id,
            ServiceName = service.Name,
            DurationMinutes = service.DurationMinutes,
            Price = service.CalculatePrice(),
            Start = start
        };

    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string PatientId { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string DoctorId { get; init; } = "";
    public string DoctorName { get; init; } = "";
    public string InstitutionId { get; init; } = "";
    public string BranchId { get; init; } = "";
    public string BranchName { get; init; } = "";
    public string ServiceId { get; init; } = "";
    public string ServiceName { get; init; } = "";
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public DateTime Start { get; init; }

    [JsonInclude] public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;

    [JsonIgnore] public DateTime End => Start.AddMinutes(DurationMinutes);

    /// <summary>Pending and confirmed appointments occupy the doctor's calendar.</summary>
    [JsonIgnore]
    public bool IsActive => Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

    public bool Overlaps(DateTime start, DateTime end) => Start < end && start < End;

    public void Confirm()
    {
        if (Status != AppointmentStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen randevu onaylanabilir.");
        Status = AppointmentStatus.Confirmed;
    }

    public void Reject()
    {
        if (Status != AppointmentStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen randevu reddedilebilir.");
        Status = AppointmentStatus.Rejected;
    }

    public void Cancel()
    {
        if (!IsActive)
            throw new InvalidOperationException("Bu randevu zaten sonlanmış.");
        Status = AppointmentStatus.Cancelled;
    }

    public override string ToString() =>
        $"[{Id}] {Start:dd.MM.yyyy HH:mm}-{End:HH:mm} | {BranchName} | {ServiceName} | Dr. {DoctorName} | {PatientName} | {Price:N2} TL | {StatusText(Status)}";

    public static string StatusText(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Pending => "Onay bekliyor",
        AppointmentStatus.Confirmed => "Onaylandı",
        AppointmentStatus.Rejected => "Reddedildi",
        _ => "İptal"
    };
}
