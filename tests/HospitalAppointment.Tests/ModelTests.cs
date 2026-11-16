using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Tests;

public class ModelTests
{
    [Fact]
    public void Services_PolymorphicPricing()
    {
        MedicalService[] services =
        [
            new Examination { Name = "M", BasePrice = 100 },
            new Radiology { Name = "R", BasePrice = 100 },
            new Laboratory { Name = "L", BasePrice = 100 }
        ];

        Assert.Equal([100m, 120m, 125m], services.Select(s => s.CalculatePrice()));
    }

    [Fact]
    public void Service_RejectsInvalidValues()
    {
        var s = new Examination();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.DurationMinutes = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => s.BasePrice = -1);
        Assert.Throws<ArgumentException>(() => s.Name = " ");
    }

    [Fact]
    public void Users_ExposeTheirOwnRoleName()
    {
        User[] users = [new Patient(), new Doctor(), new Nurse(), new Admin(), new Admin { IsSuperAdmin = true }];
        Assert.Equal(["Hasta", "Doktor", "Sağlık Personeli", "Hastane/Klinik Yöneticisi", "Sistem Yöneticisi"],
            users.Select(u => u.RoleName));
    }

    [Fact]
    public void Password_IsHashedAndVerified()
    {
        var user = new Patient { Username = "x", FullName = "X" };
        user.SetPassword("secret1");

        Assert.NotEqual("secret1", user.PasswordHash);
        Assert.True(user.VerifyPassword("secret1"));
        Assert.False(user.VerifyPassword("wrong"));
    }

    [Fact]
    public void Doctor_RejectsOverlappingAvailability()
    {
        var d = new Doctor();
        d.AddAvailability(new AvailabilitySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)));

        Assert.Throws<InvalidOperationException>(() =>
            d.AddAvailability(new AvailabilitySlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0))));
        d.AddAvailability(new AvailabilitySlot(DayOfWeek.Tuesday, new TimeOnly(11, 0), new TimeOnly(13, 0)));
        Assert.Equal(2, d.Availability.Count);
    }

    [Fact]
    public void Appointment_StatusFlowIsEnforced()
    {
        var doctor = new Doctor { FullName = "D" };
        var patient = new Patient { FullName = "P" };
        var a = Appointment.Create(patient, doctor, new Branch { Name = "B" },
            new Examination { Name = "M", BasePrice = 10 }, new DateTime(2030, 1, 8, 10, 0, 0));

        a.Cancel();
        Assert.Equal(AppointmentStatus.Cancelled, a.Status);
        Assert.Throws<InvalidOperationException>(a.Confirm);
        Assert.Throws<InvalidOperationException>(a.Cancel);
    }

    [Fact]
    public void JsonRepository_PersistsPolymorphicUsersAndServices()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hospital-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var usersPath = Path.Combine(dir, "users.json");
            var instPath = Path.Combine(dir, "inst.json");

            var users = new JsonRepository<User>(usersPath);
            var doctor = new Doctor { Username = "d", FullName = "Doc", Specialty = "KBB" };
            doctor.AddService("s1");
            doctor.AddAvailability(new AvailabilitySlot(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(10, 0)));
            doctor.SetPassword("1234");
            users.Add(doctor);
            users.Add(new Patient { Username = "p", FullName = "Pat" });

            var insts = new JsonRepository<Institution>(instPath);
            var branch = new Branch { Name = "B", Services = [new Radiology { Id = "s1", Name = "Rontgen", BasePrice = 100 }] };
            insts.Add(new Institution { Name = "I", Branches = [branch] });

            var users2 = new JsonRepository<User>(usersPath);
            var loadedDoctor = Assert.IsType<Doctor>(users2.GetById(doctor.Id));
            Assert.IsType<Patient>(users2.GetAll().Single(u => u.Username == "p"));
            Assert.Equal("KBB", loadedDoctor.Specialty);
            Assert.True(loadedDoctor.Offers("s1"));
            Assert.Single(loadedDoctor.Availability);
            Assert.True(loadedDoctor.VerifyPassword("1234"));

            var loadedService = new JsonRepository<Institution>(instPath).GetAll().Single().Branches.Single().Services.Single();
            Assert.IsType<Radiology>(loadedService);
            Assert.Equal(120m, loadedService.CalculatePrice());
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void JsonRepository_PersistsAppointmentStatus()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hospital-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(dir, "appointments.json");
            var repo = new JsonRepository<Appointment>(path);
            var a = Appointment.Create(new Patient { FullName = "P" }, new Doctor { FullName = "D" }, new Branch { Name = "B" },
                new Examination { Name = "M", DurationMinutes = 20, BasePrice = 10 }, new DateTime(2030, 1, 8, 10, 0, 0));
            repo.Add(a);
            a.Confirm();
            repo.Update(a);

            var loaded = new JsonRepository<Appointment>(path).GetAll().Single();
            Assert.Equal(AppointmentStatus.Confirmed, loaded.Status);
            Assert.Equal(20, loaded.DurationMinutes);
            Assert.Equal(new DateTime(2030, 1, 8, 10, 20, 0), loaded.End);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
