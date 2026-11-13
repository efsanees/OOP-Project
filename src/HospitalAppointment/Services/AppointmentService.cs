using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Services;

/// <summary>Booking rules: availability, working hours, conflict detection and the approval workflow.</summary>
public class AppointmentService
{
    public const int SlotStepMinutes = 15;

    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<User> _users;
    private readonly IRepository<Institution> _institutions;
    private readonly Func<DateTime> _clock;

    public AppointmentService(
        IRepository<Appointment> appointments,
        IRepository<User> users,
        IRepository<Institution> institutions,
        Func<DateTime>? clock = null)
    {
        _appointments = appointments;
        _users = users;
        _institutions = institutions;
        _clock = clock ?? (() => DateTime.Now);
    }

    public Appointment Book(string patientId, string doctorId, string serviceId, DateTime start)
    {
        var patient = _users.GetById(patientId) as Patient
            ?? throw new AppointmentException("Hasta bulunamadı.");
        var doctor = _users.GetById(doctorId) as Doctor
            ?? throw new AppointmentException("Doktor bulunamadı.");
        var branch = _institutions.GetById(doctor.InstitutionId)?.FindBranch(doctor.BranchId)
            ?? throw new AppointmentException("Doktorun şubesi bulunamadı.");
        var service = branch.FindService(serviceId)
            ?? throw new AppointmentException("Hizmet bu şubede sunulmuyor.");

        if (!doctor.Offers(serviceId))
            throw new AppointmentException("Bu doktor seçilen hizmeti vermiyor.");
        if (start <= _clock())
            throw new AppointmentException("Geçmiş bir zamana randevu alınamaz.");

        var end = start.AddMinutes(service.DurationMinutes);

        if (!branch.IsOpen(start, end))
            throw new AppointmentException("Şube bu saatte kapalı.");
        if (!doctor.IsAvailable(start, end))
            throw new AppointmentException("Doktor bu zaman aralığında müsait değil.");
        if (HasConflict(doctor.Id, start, end))
            throw new AppointmentException("Doktorun bu saatte başka bir randevusu var (çakışma).");
        if (_appointments.GetAll().Any(a => a.PatientId == patient.Id && a.IsActive && a.Overlaps(start, end)))
            throw new AppointmentException("Aynı saatte başka bir randevunuz var.");

        var appointment = Appointment.Create(patient, doctor, branch, service, start);
        _appointments.Add(appointment);
        return appointment;
    }

    /// <summary>True if an active appointment of the doctor overlaps [start, end).</summary>
    public bool HasConflict(string doctorId, DateTime start, DateTime end) =>
        _appointments.GetAll().Any(a => a.DoctorId == doctorId && a.IsActive && a.Overlaps(start, end));

    /// <summary>All start times on <paramref name="date"/> that could currently be booked.</summary>
    public IReadOnlyList<DateTime> GetFreeStarts(string doctorId, string serviceId, DateOnly date)
    {
        var result = new List<DateTime>();
        if (_users.GetById(doctorId) is not Doctor doctor || !doctor.Offers(serviceId)) return result;
        var branch = _institutions.GetById(doctor.InstitutionId)?.FindBranch(doctor.BranchId);
        var service = branch?.FindService(serviceId);
        if (branch is null || service is null) return result;

        foreach (var slot in doctor.Availability.Where(s => s.Day == date.DayOfWeek))
        {
            var candidate = date.ToDateTime(slot.Start);
            var slotEnd = date.ToDateTime(slot.End);
            while (candidate.AddMinutes(service.DurationMinutes) <= slotEnd)
            {
                var end = candidate.AddMinutes(service.DurationMinutes);
                if (candidate > _clock() && branch.IsOpen(candidate, end) && !HasConflict(doctorId, candidate, end))
                    result.Add(candidate);
                candidate = candidate.AddMinutes(SlotStepMinutes);
            }
        }
        return result.OrderBy(d => d).ToList();
    }

    public Appointment Confirm(string appointmentId, User actor)
    {
        var appointment = GetManageable(appointmentId, actor, allowPatient: false);
        appointment.Confirm();
        _appointments.Update(appointment);
        return appointment;
    }

    public Appointment Reject(string appointmentId, User actor)
    {
        var appointment = GetManageable(appointmentId, actor, allowPatient: false);
        appointment.Reject();
        _appointments.Update(appointment);
        return appointment;
    }

    public Appointment Cancel(string appointmentId, User actor)
    {
        var appointment = GetManageable(appointmentId, actor, allowPatient: true);
        appointment.Cancel();
        _appointments.Update(appointment);
        return appointment;
    }

    public IReadOnlyList<Appointment> ForPatient(string patientId) =>
        _appointments.GetAll().Where(a => a.PatientId == patientId).OrderBy(a => a.Start).ToList();

    public IReadOnlyList<Appointment> ForDoctor(string doctorId) =>
        _appointments.GetAll().Where(a => a.DoctorId == doctorId).OrderBy(a => a.Start).ToList();

    public IReadOnlyList<Appointment> ForInstitution(string institutionId) =>
        _appointments.GetAll().Where(a => a.InstitutionId == institutionId).OrderBy(a => a.Start).ToList();

    public IReadOnlyList<Appointment> ForBranch(string institutionId, string branchId) =>
        ForInstitution(institutionId).Where(a => a.BranchId == branchId).ToList();

    /// <summary>Loads an appointment and checks that the actor is allowed to change it.</summary>
    private Appointment GetManageable(string appointmentId, User actor, bool allowPatient)
    {
        var appointment = _appointments.GetById(appointmentId)
            ?? throw new AppointmentException("Randevu bulunamadı.");

        var allowed = actor switch
        {
            Doctor d => appointment.DoctorId == d.Id,
            // Health personnel handle the appointments of the branch they work at.
            Nurse n => appointment.InstitutionId == n.InstitutionId && appointment.BranchId == n.BranchId,
            Admin a => a.IsSuperAdmin || appointment.InstitutionId == a.InstitutionId,
            Patient p => allowPatient && appointment.PatientId == p.Id,
            _ => false
        };
        if (!allowed)
            throw new AppointmentException("Bu randevu üzerinde işlem yapma yetkiniz yok.");
        return appointment;
    }
}
