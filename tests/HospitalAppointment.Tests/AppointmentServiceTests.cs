using HospitalAppointment.Models;
using HospitalAppointment.Services;

namespace HospitalAppointment.Tests;

public class AppointmentServiceTests
{
    // Fixed "now": Monday 2030-01-07 08:00. Tests book on Tuesday 2030-01-08.
    private static readonly DateTime Now = new(2030, 1, 7, 8, 0, 0);
    private static readonly DateTime Tuesday10 = new(2030, 1, 8, 10, 0, 0);

    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<Institution> _institutions = new();
    private readonly InMemoryRepository<Appointment> _appointments = new();
    private readonly AppointmentService _service;
    private readonly Patient _patient;
    private readonly Patient _otherPatient;
    private readonly Doctor _doctor;
    private readonly Admin _admin;

    public AppointmentServiceTests()
    {
        var catalog = new CatalogService(_institutions, _users);
        var inst = catalog.AddInstitution("Test Hastanesi");
        var branch = catalog.AddBranch(inst.Id, "Merkez", "Adres", new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday], ["Dahiliye"]);
        catalog.AddService(inst.Id, branch.Id, new Examination { Id = "exam", Name = "Muayene", DurationMinutes = 30, BasePrice = 500, DepartmentName = "Dahiliye" });
        catalog.AddService(inst.Id, branch.Id, new Examination { Id = "other", Name = "Baska", DurationMinutes = 30, BasePrice = 100, DepartmentName = "Dahiliye" });

        _doctor = catalog.AddDoctor(inst.Id, branch.Id, "doc", "1234", "Test Doktor", "Dahiliye", "Dahiliye", ["exam"]);
        _doctor.AddAvailability(new AvailabilitySlot(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)));
        catalog.SaveDoctor(_doctor);
        _admin = catalog.AddAdmin(inst.Id, "adm", "1234", "Yonetici");

        _patient = new Patient { Username = "p1", FullName = "Hasta Bir" };
        _otherPatient = new Patient { Username = "p2", FullName = "Hasta Iki" };
        _users.Add(_patient);
        _users.Add(_otherPatient);

        _service = new AppointmentService(_appointments, _users, _institutions, () => Now);
    }

    [Fact]
    public void Book_InsideAvailability_CreatesPendingAppointmentWithSnapshot()
    {
        var a = _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);

        Assert.Equal(AppointmentStatus.Pending, a.Status);
        Assert.Equal(30, a.DurationMinutes);
        Assert.Equal(500m, a.Price);
        Assert.Equal(Tuesday10.AddMinutes(30), a.End);
        Assert.Single(_appointments.GetAll());
    }

    [Fact]
    public void Book_SameDoctorOverlappingTime_IsRejectedAsConflict()
    {
        _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);

        // 10:15 overlaps the 10:00-10:30 appointment.
        var ex = Assert.Throws<AppointmentException>(() =>
            _service.Book(_otherPatient.Id, _doctor.Id, "exam", Tuesday10.AddMinutes(15)));
        Assert.Contains("çakışma", ex.Message);
    }

    [Fact]
    public void Book_BackToBack_IsAllowed()
    {
        _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        var second = _service.Book(_otherPatient.Id, _doctor.Id, "exam", Tuesday10.AddMinutes(30));
        Assert.Equal(Tuesday10.AddMinutes(30), second.Start);
    }

    [Fact]
    public void Book_OutsideDoctorAvailability_IsRejected()
    {
        // Doctor is only available 09:00-12:00 on Tuesday; 11:45 would end at 12:15.
        Assert.Throws<AppointmentException>(() =>
            _service.Book(_patient.Id, _doctor.Id, "exam", new DateTime(2030, 1, 8, 11, 45, 0)));
        // Wrong day.
        Assert.Throws<AppointmentException>(() =>
            _service.Book(_patient.Id, _doctor.Id, "exam", new DateTime(2030, 1, 14, 10, 0, 0)));
    }

    [Fact]
    public void Book_ServiceDoctorDoesNotOffer_IsRejected()
    {
        Assert.Throws<AppointmentException>(() =>
            _service.Book(_patient.Id, _doctor.Id, "other", Tuesday10));
    }

    [Fact]
    public void Book_InThePast_IsRejected()
    {
        var late = new AppointmentService(_appointments, _users, _institutions, () => Tuesday10.AddHours(1));
        Assert.Throws<AppointmentException>(() => late.Book(_patient.Id, _doctor.Id, "exam", Tuesday10));
    }

    [Fact]
    public void Book_PatientAlreadyBusy_IsRejected()
    {
        var second = new Doctor { Username = "doc2", FullName = "Ikinci", InstitutionId = _doctor.InstitutionId, BranchId = _doctor.BranchId };
        second.AddService("exam");
        second.AddAvailability(new AvailabilitySlot(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)));
        _users.Add(second);

        _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        Assert.Throws<AppointmentException>(() => _service.Book(_patient.Id, second.Id, "exam", Tuesday10));
    }

    [Fact]
    public void CancelledAppointment_FreesTheTimeSlot()
    {
        var a = _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        _service.Cancel(a.Id, _patient);

        var again = _service.Book(_otherPatient.Id, _doctor.Id, "exam", Tuesday10);
        Assert.Equal(AppointmentStatus.Pending, again.Status);
    }

    [Fact]
    public void Doctor_CanConfirmOwnAppointment_ButConfirmedCannotBeConfirmedAgain()
    {
        var a = _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        _service.Confirm(a.Id, _doctor);

        Assert.Equal(AppointmentStatus.Confirmed, _appointments.GetById(a.Id)!.Status);
        Assert.Throws<InvalidOperationException>(() => _service.Confirm(a.Id, _doctor));
    }

    [Fact]
    public void Patient_CannotConfirm_AndOtherPatientCannotCancel()
    {
        var a = _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);

        Assert.Throws<AppointmentException>(() => _service.Confirm(a.Id, _patient));
        Assert.Throws<AppointmentException>(() => _service.Cancel(a.Id, _otherPatient));
    }

    [Fact]
    public void Admin_CanRejectAppointmentOfOwnInstitution()
    {
        var a = _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        _service.Reject(a.Id, _admin);
        Assert.Equal(AppointmentStatus.Rejected, _appointments.GetById(a.Id)!.Status);
    }

    [Fact]
    public void GetFreeStarts_ExcludesBookedTimes()
    {
        var date = DateOnly.FromDateTime(Tuesday10);
        var before = _service.GetFreeStarts(_doctor.Id, "exam", date);
        Assert.Contains(Tuesday10, before);

        _service.Book(_patient.Id, _doctor.Id, "exam", Tuesday10);
        var after = _service.GetFreeStarts(_doctor.Id, "exam", date);

        Assert.DoesNotContain(Tuesday10, after);
        Assert.DoesNotContain(Tuesday10.AddMinutes(15), after);
        Assert.Contains(Tuesday10.AddMinutes(30), after);
        Assert.DoesNotContain(new DateTime(2030, 1, 8, 11, 45, 0), after); // would run past 12:00
    }
}
