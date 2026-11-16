using HospitalAppointment.Models;
using HospitalAppointment.Services;

namespace HospitalAppointment.UI;

/// <summary>
/// Shared menu loop (template method): subclasses only supply a title and their options.
/// Business-rule errors are shown to the user instead of crashing the program.
/// </summary>
public abstract class MenuBase(AppServices app) : IMenu
{
    protected AppServices App { get; } = app;

    protected abstract string Title { get; }

    /// <summary>The menu entries of this role, numbered from 1 in the order given.</summary>
    protected abstract IReadOnlyList<(string Label, Action Action)> Options();

    public void Run()
    {
        while (true)
        {
            ConsoleUi.Title(Title);
            var options = Options();
            for (var i = 0; i < options.Count; i++) Console.WriteLine($"{i + 1}) {options[i].Label}");
            Console.WriteLine("0) Çıkış (oturumu kapat)");

            var choice = ConsoleUi.ReadInt("Seçiminiz", 0, options.Count);
            if (choice == 0) return;
            try
            {
                options[choice - 1].Action();
            }
            catch (Exception ex) when (ex is AppointmentException or InvalidOperationException or ArgumentException)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }
    }

    /// <summary>Lets <paramref name="actor"/> confirm or reject one of the given pending appointments.</summary>
    protected void DecidePending(IEnumerable<Appointment> appointments, User actor)
    {
        var pending = appointments.Where(a => a.Status == AppointmentStatus.Pending).ToList();
        var selected = ConsoleUi.Choose("Bekleyen randevular", pending, a => a.ToString());
        if (selected is null) return;
        Console.WriteLine("1) Onayla   2) Reddet   0) Vazgeç");
        switch (ConsoleUi.ReadInt("Seçiminiz", 0, 2))
        {
            case 1: App.Booking.Confirm(selected.Id, actor); Console.WriteLine("Randevu onaylandı."); break;
            case 2: App.Booking.Reject(selected.Id, actor); Console.WriteLine("Randevu reddedildi."); break;
        }
    }
}
