using HospitalAppointment.Models;

namespace HospitalAppointment.UI;

public class PatientMenu(Patient patient, AppServices app) : MenuBase(app)
{
    protected override string Title => $"Hasta Menüsü - {patient.FullName}";

    protected override IReadOnlyList<(string Label, Action Action)> Options() =>
    [
        ("Randevu al", BookAppointment),
        ("Randevularım", () => ConsoleUi.PrintAppointments(App.Booking.ForPatient(patient.Id))),
        ("Randevu iptal et", CancelAppointment)
    ];

    private void BookAppointment()
    {
        var institution = ConsoleUi.Choose("Kurum seçin", App.Catalog.Institutions, i => i.Name);
        if (institution is null) return;
        var branch = ConsoleUi.Choose("Şube seçin", institution.Branches, b => b.ToString());
        if (branch is null) return;
        var service = ConsoleUi.Choose("Hizmet seçin", branch.Services, s => s.ToString());
        if (service is null) return;

        var doctors = App.Catalog.DoctorsOffering(institution.Id, branch.Id, service.Id);
        var doctor = ConsoleUi.Choose("Doktor seçin", doctors, d => d.Describe());
        if (doctor is null) return;

        if (doctor.Availability.Count > 0)
        {
            Console.WriteLine("Doktorun haftalık müsaitliği:");
            ConsoleUi.PrintAvailability(doctor);
        }
        var date = ConsoleUi.ReadDate("Randevu tarihi");
        var starts = App.Booking.GetFreeStarts(doctor.Id, service.Id, date);
        if (starts.Count == 0)
        {
            Console.WriteLine("Bu gün için uygun saat yok.");
            return;
        }
        var index = ConsoleUi.ChooseIndex("Uygun saatler", starts, s => s.ToString("HH:mm"));
        if (index < 0) return;

        var appointment = App.Booking.Book(patient.Id, doctor.Id, service.Id, starts[index]);
        Console.WriteLine("Randevu oluşturuldu, onay bekleniyor:");
        Console.WriteLine("  " + appointment);
    }

    private void CancelAppointment()
    {
        var active = App.Booking.ForPatient(patient.Id).Where(a => a.IsActive).ToList();
        var selected = ConsoleUi.Choose("İptal edilecek randevu", active, a => a.ToString());
        if (selected is null) return;
        App.Booking.Cancel(selected.Id, patient);
        Console.WriteLine("Randevu iptal edildi.");
    }
}
