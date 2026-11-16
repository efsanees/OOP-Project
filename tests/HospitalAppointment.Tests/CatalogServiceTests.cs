using HospitalAppointment.Models;
using HospitalAppointment.Services;
using HospitalAppointment.UI;

namespace HospitalAppointment.Tests;

public class CatalogServiceTests
{
    // Fixed "now": Monday 2030-01-07 08:00. Bookings are made on Tuesday 2030-01-08.
    private static readonly DateTime Now = new(2030, 1, 7, 8, 0, 0);
    private static readonly DateTime Tuesday10 = new(2030, 1, 8, 10, 0, 0);

    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<Institution> _institutions = new();
    private readonly InMemoryRepository<Appointment> _appointments = new();
    private readonly CatalogService _catalog;
    private readonly AppointmentService _booking;
    private readonly Institution _inst;
    private readonly Branch _merkez;
    private readonly Branch _sube2;
    private readonly Doctor _doctor;
    private readonly Patient _patient;

    public CatalogServiceTests()
    {
        _catalog = new CatalogService(_institutions, _users);
        _booking = new AppointmentService(_appointments, _users, _institutions, () => Now);
        _inst = _catalog.AddInstitution("Test Hastanesi");
        _merkez = _catalog.AddBranch(_inst.Id, "Merkez", "", new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday], ["Dahiliye", "Laboratuvar"]);
        _sube2 = _catalog.AddBranch(_inst.Id, "Şube 2", "", new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Tuesday], ["Dahiliye"]);
        _catalog.AddService(_inst.Id, _merkez.Id, new Examination { Id = "exam", Name = "Muayene", DurationMinutes = 30, BasePrice = 500, DepartmentName = "Dahiliye" });
        _catalog.AddService(_inst.Id, _merkez.Id, new Laboratory { Id = "lab", Name = "Tahlil", DurationMinutes = 15, BasePrice = 100, DepartmentName = "Laboratuvar" });

        _doctor = _catalog.AddDoctor(_inst.Id, _merkez.Id, "doc", "1234", "Test Doktor", "Dahiliye", "Dahiliye", ["exam"]);
        _catalog.AddAvailability(_doctor, new AvailabilitySlot(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)));

        _patient = new Patient { Username = "p1", FullName = "Hasta Bir" };
        _users.Add(_patient);
    }

    [Fact]
    public void AddNurse_RegistersHealthPersonnelWithOwnRole()
    {
        var nurse = _catalog.AddNurse(_inst.Id, _merkez.Id, "zehra", "1234", "Zehra Demir", "Laborant", "Laboratuvar");

        Assert.Same(nurse, _users.GetById(nurse.Id));
        Assert.Equal("Sağlık Personeli", nurse.RoleName);
        Assert.Equal("Laborant", nurse.Position);
        Assert.Equal(_merkez.Id, nurse.BranchId);
        Assert.True(nurse.VerifyPassword("1234"));
        Assert.Single(_catalog.NursesOf(_inst.Id));
        Assert.IsType<NurseMenu>(MenuFactory.For(nurse, null!));
    }

    [Fact]
    public void AddNurse_DuplicateUsernameOrUnknownDepartment_IsRejected()
    {
        Assert.Throws<AppointmentException>(() =>
            _catalog.AddNurse(_inst.Id, _merkez.Id, "DOC", "1234", "Biri", "Hemşire", "Dahiliye"));
        Assert.Throws<AppointmentException>(() =>
            _catalog.AddNurse(_inst.Id, _merkez.Id, "yeni", "1234", "Biri", "Hemşire", "Kardiyoloji"));
        Assert.Empty(_catalog.NursesOf(_inst.Id));
    }

    [Fact]
    public void Nurse_CanConfirmAppointmentsOfOwnBranchOnly()
    {
        var own = _catalog.AddNurse(_inst.Id, _merkez.Id, "nurse1", "1234", "Merkez Hemşire", "Hemşire", "Dahiliye");
        var other = _catalog.AddNurse(_inst.Id, _sube2.Id, "nurse2", "1234", "Şube Hemşire", "Hemşire", "Dahiliye");
        var a = _booking.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);

        Assert.Throws<AppointmentException>(() => _booking.Confirm(a.Id, other));
        _booking.Confirm(a.Id, own);

        Assert.Equal(AppointmentStatus.Confirmed, _appointments.GetById(a.Id)!.Status);
        Assert.Single(_booking.ForBranch(_inst.Id, _merkez.Id));
        Assert.Empty(_booking.ForBranch(_inst.Id, _sube2.Id));
    }

    [Fact]
    public void AddAvailability_OutsideBranchHoursOrDays_IsRejected()
    {
        // Branch is open 08:00-17:00, Monday and Tuesday only.
        Assert.Throws<AppointmentException>(() =>
            _catalog.AddAvailability(_doctor, new AvailabilitySlot(DayOfWeek.Tuesday, new TimeOnly(16, 0), new TimeOnly(18, 0))));
        Assert.Throws<AppointmentException>(() =>
            _catalog.AddAvailability(_doctor, new AvailabilitySlot(DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(12, 0))));

        _catalog.AddAvailability(_doctor, new AvailabilitySlot(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(17, 0)));
        Assert.Equal(2, _doctor.Availability.Count);
    }

    [Fact]
    public void AssignService_MakesNewServiceBookable_UnassignStopsIt()
    {
        Assert.Throws<AppointmentException>(() => _booking.Book(_patient.Id, _doctor.Id, "lab", Tuesday10));

        _catalog.AssignService(_doctor, "lab");
        var a = _booking.Book(_patient.Id, _doctor.Id, "lab", Tuesday10);
        Assert.Equal(125m, a.Price); // laboratory: base price + 25 TL sample fee

        _catalog.UnassignService(_doctor, "lab");
        Assert.False(_doctor.Offers("lab"));
        Assert.Throws<AppointmentException>(() => _catalog.AssignService(_doctor, "yok"));
    }

    [Fact]
    public void AddInstitutionWithAdmin_InvalidAdmin_SavesNothing()
    {
        var before = _institutions.GetAll().Count;

        Assert.Throws<AppointmentException>(() => _catalog.AddInstitutionWithAdmin("Yeni Klinik", "Yönetici", "doc", "1234")); // username taken
        Assert.Throws<ArgumentException>(() => _catalog.AddInstitutionWithAdmin("Yeni Klinik", "Yönetici", "yonetici", "12")); // short password
        Assert.Equal(before, _institutions.GetAll().Count);

        var (inst, admin) = _catalog.AddInstitutionWithAdmin("Yeni Klinik", "Yönetici", "yonetici", "1234");
        Assert.Equal(inst.Id, admin.InstitutionId);
        Assert.NotNull(_institutions.GetById(inst.Id));
        Assert.NotNull(_users.GetById(admin.Id));
    }

    [Fact]
    public void ConsoleInput_WhenInputEnds_StopsInsteadOfLoopingForever()
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        try
        {
            Console.SetIn(new StringReader("abc\n"));
            Console.SetOut(TextWriter.Null);
            Assert.Throws<InputClosedException>(() => ConsoleUi.ReadInt("Seçiminiz", 0, 2));
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }
}
