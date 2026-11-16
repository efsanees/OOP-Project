using HospitalAppointment.Models;

namespace HospitalAppointment.UI;

/// <summary>Health personnel: follow the branch's appointment list and confirm / reject requests.</summary>
public class NurseMenu(Nurse nurse, AppServices app) : MenuBase(app)
{
    protected override string Title =>
        $"Sağlık Personeli Menüsü - {nurse.Describe()} - {App.Catalog.BranchOf(nurse)?.Name ?? "şube yok"}";

    protected override IReadOnlyList<(string Label, Action Action)> Options() =>
    [
        ("Şubedeki tüm randevular", () => ConsoleUi.PrintAppointments(BranchAppointments())),
        ("Bir günün randevu listesi", DailyList),
        ("Bekleyen randevuları onayla / reddet", () => DecidePending(BranchAppointments(), nurse))
    ];

    private IReadOnlyList<Appointment> BranchAppointments() =>
        App.Booking.ForBranch(nurse.InstitutionId, nurse.BranchId);

    private void DailyList()
    {
        var date = ConsoleUi.ReadDate("Tarih");
        ConsoleUi.PrintAppointments(BranchAppointments().Where(a => DateOnly.FromDateTime(a.Start) == date).ToList());
    }
}
