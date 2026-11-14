using System.Globalization;
using HospitalAppointment.Models;

namespace HospitalAppointment.UI;

/// <summary>Thrown when standard input is closed (Ctrl+Z / end of a piped file); the program then exits.</summary>
public class InputClosedException() : Exception("Girdi akışı kapandı.");

/// <summary>Small helpers for reading validated input and printing lists.</summary>
public static class ConsoleUi
{
    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {text} ===");
    }

    /// <summary>Reads one line; ends the program instead of looping forever when input is closed.</summary>
    private static string ReadLine() => Console.ReadLine() ?? throw new InputClosedException();

    public static string ReadString(string label, bool allowEmpty = false)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var input = ReadLine().Trim();
            if (allowEmpty || input.Length > 0) return input;
            Console.WriteLine("Boş bırakılamaz.");
        }
    }

    public static int ReadInt(string label, int min, int max)
    {
        while (true)
        {
            Console.Write($"{label} ({min}-{max}): ");
            if (int.TryParse(ReadLine().Trim(), out var value) && value >= min && value <= max) return value;
            Console.WriteLine("Geçersiz sayı.");
        }
    }

    public static decimal ReadDecimal(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var text = ReadLine().Trim().Replace(',', '.');
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0)
                return value;
            Console.WriteLine("Geçersiz tutar.");
        }
    }

    public static DateOnly ReadDate(string label)
    {
        while (true)
        {
            Console.Write($"{label} (gg.aa.yyyy): ");
            if (DateOnly.TryParseExact(ReadLine().Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                return date;
            Console.WriteLine("Tarih biçimi geçersiz.");
        }
    }

    public static TimeOnly ReadTime(string label)
    {
        while (true)
        {
            Console.Write($"{label} (ss:dd): ");
            if (TimeOnly.TryParseExact(ReadLine().Trim(), "HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var time))
                return time;
            Console.WriteLine("Saat biçimi geçersiz.");
        }
    }

    public static DayOfWeek ReadDay(string label)
    {
        // Monday first, as in Turkish calendars.
        DayOfWeek[] days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        for (var i = 0; i < days.Length; i++) Console.WriteLine($"  {i + 1}) {DayName(days[i])}");
        return days[ReadInt(label, 1, days.Length) - 1];
    }

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        _ => "Pazar"
    };

    /// <summary>Shows a numbered list and returns the chosen index, or -1 when the list is empty or the user picks 0 (back).</summary>
    public static int ChooseIndex<T>(string title, IReadOnlyList<T> items, Func<T, string> label)
    {
        if (items.Count == 0)
        {
            Console.WriteLine($"{title}: kayıt yok.");
            return -1;
        }
        Console.WriteLine($"{title}:");
        for (var i = 0; i < items.Count; i++) Console.WriteLine($"  {i + 1}) {label(items[i])}");
        Console.WriteLine("  0) Geri");
        return ReadInt("Seçiminiz", 0, items.Count) - 1;
    }

    /// <summary>Shows a numbered list and returns the choice, or null when the user picks 0 (back).</summary>
    public static T? Choose<T>(string title, IReadOnlyList<T> items, Func<T, string> label) where T : class
    {
        var index = ChooseIndex(title, items, label);
        return index < 0 ? null : items[index];
    }

    public static void PrintAppointments(IReadOnlyList<Appointment> appointments)
    {
        if (appointments.Count == 0)
        {
            Console.WriteLine("Randevu yok.");
            return;
        }
        foreach (var a in appointments) Console.WriteLine("  " + a);
    }

    /// <summary>Prints the doctor's weekly availability windows, numbered from 1.</summary>
    public static void PrintAvailability(Doctor doctor)
    {
        if (doctor.Availability.Count == 0) Console.WriteLine("Tanımlı müsaitlik yok.");
        for (var i = 0; i < doctor.Availability.Count; i++)
        {
            var s = doctor.Availability[i];
            Console.WriteLine($"  {i + 1}) {DayName(s.Day)} {s.Start:HH:mm}-{s.End:HH:mm}");
        }
    }

    /// <summary>Asks for a day and a time range (used by both the doctor and the admin menus).</summary>
    public static AvailabilitySlot ReadAvailabilitySlot()
    {
        var day = ReadDay("Gün");
        var start = ReadTime("Başlangıç");
        var end = ReadTime("Bitiş");
        return new AvailabilitySlot(day, start, end);
    }
}
