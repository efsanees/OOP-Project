using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Services;

/// <summary>Creates demo data the first time the program runs (empty data folder).</summary>
public static class SeedData
{
    public static void EnsureSeeded(IRepository<Institution> institutions, IRepository<User> users, CatalogService catalog)
    {
        if (institutions.GetAll().Count > 0 || users.GetAll().Count > 0) return;

        var inst = catalog.AddInstitution("Sakarya Şehir Hastanesi");
        var depts = new[] { "Dahiliye", "Radyoloji", "Laboratuvar" };

        var merkez = catalog.AddBranch(inst.Id, "Merkez Şube", "Adapazarı",
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], depts);
        var serdivan = catalog.AddBranch(inst.Id, "Serdivan Şube", "Serdivan",
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
            depts);

        var genel = new Examination { Id = "genel", Name = "Genel Muayene", DurationMinutes = 15, BasePrice = 500, DepartmentName = "Dahiliye" };
        var dahiliye = new Examination { Id = "dahiliye", Name = "Dahiliye Muayenesi", DurationMinutes = 30, BasePrice = 750, DepartmentName = "Dahiliye" };
        var rontgen = new Radiology { Id = "rontgen", Name = "Röntgen", DurationMinutes = 15, BasePrice = 300, DepartmentName = "Radyoloji" };
        var tahlil = new Laboratory { Id = "tahlil", Name = "Kan Tahlili", DurationMinutes = 15, BasePrice = 200, DepartmentName = "Laboratuvar" };
        foreach (var s in new MedicalService[] { genel, dahiliye, rontgen })
            catalog.AddService(inst.Id, merkez.Id, s);
        catalog.AddService(inst.Id, merkez.Id, tahlil);

        catalog.AddService(inst.Id, serdivan.Id, new Examination { Id = "genel", Name = "Genel Muayene", DurationMinutes = 20, BasePrice = 550, DepartmentName = "Dahiliye" });
        catalog.AddService(inst.Id, serdivan.Id, new Radiology { Id = "rontgen", Name = "Röntgen", DurationMinutes = 15, BasePrice = 320, DepartmentName = "Radyoloji" });

        var ayse = catalog.AddDoctor(inst.Id, merkez.Id, "ayse", "1234", "Ayşe Yılmaz", "Dahiliye", "Dahiliye", ["genel", "dahiliye", "tahlil"]);
        var mehmet = catalog.AddDoctor(inst.Id, serdivan.Id, "mehmet", "1234", "Mehmet Kaya", "Radyoloji", "Radyoloji", ["genel", "rontgen"]);
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            catalog.AddAvailability(ayse, new AvailabilitySlot(day, new TimeOnly(9, 0), new TimeOnly(12, 0)));
            catalog.AddAvailability(ayse, new AvailabilitySlot(day, new TimeOnly(13, 0), new TimeOnly(16, 0)));
            catalog.AddAvailability(mehmet, new AvailabilitySlot(day, new TimeOnly(10, 0), new TimeOnly(17, 0)));
        }

        catalog.AddNurse(inst.Id, merkez.Id, "zehra", "1234", "Zehra Demir", "Hemşire", "Dahiliye");

        catalog.AddAdmin(inst.Id, "admin", "1234", "Kurum Yöneticisi");
        catalog.AddAdmin(inst.Id, "super", "1234", "Sistem Yöneticisi", isSuperAdmin: true);

        var patient = new Patient { Username = "hasta", FullName = "Ali Veli", Phone = "0500 000 00 00" };
        patient.SetPassword("1234");
        users.Add(patient);
    }
}
