using HospitalAppointment.Models;

namespace HospitalAppointment.UI;

public class DoctorMenu(Doctor doctor, AppServices app) : MenuBase(app)
{
    protected override string Title => $"Doktor Menüsü - {doctor.Describe()}";

    protected override IReadOnlyList<(string Label, Action Action)> Options() =>
    [
        ("Tüm randevularım", () => ConsoleUi.PrintAppointments(App.Booking.ForDoctor(doctor.Id))),
        ("Bekleyen randevuları onayla / reddet", () => DecidePending(App.Booking.ForDoctor(doctor.Id), doctor)),
        ("Müsaitlik saatlerim", () => ConsoleUi.PrintAvailability(doctor)),
        ("Müsaitlik aralığı ekle", AddAvailability),
        ("Müsaitlik aralığı sil", RemoveAvailability)
    ];

    private void AddAvailability()
    {
        App.Catalog.AddAvailability(doctor, ConsoleUi.ReadAvailabilitySlot());
        Console.WriteLine("Müsaitlik eklendi.");
    }

    private void RemoveAvailability()
    {
        if (doctor.Availability.Count == 0) { Console.WriteLine("Silinecek aralık yok."); return; }
        ConsoleUi.PrintAvailability(doctor);
        var index = ConsoleUi.ReadInt("Silinecek numara", 1, doctor.Availability.Count);
        App.Catalog.RemoveAvailability(doctor, index - 1);
        Console.WriteLine("Aralık silindi.");
    }
}
