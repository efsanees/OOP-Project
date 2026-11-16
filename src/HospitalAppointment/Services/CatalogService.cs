using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Services;

/// <summary>Management operations used by admins: institutions, branches, services and staff.</summary>
public class CatalogService(IRepository<Institution> institutions, IRepository<User> users)
{
    public IReadOnlyList<Institution> Institutions => institutions.GetAll();

    public Institution? GetInstitution(string id) => institutions.GetById(id);

    public Institution AddInstitution(string name)
    {
        var institution = NewInstitution(name);
        institutions.Add(institution);
        return institution;
    }

    /// <summary>
    /// Creates an institution together with its manager. Everything is validated before anything
    /// is saved, so a bad username/password never leaves an institution without a manager behind.
    /// </summary>
    public (Institution Institution, Admin Admin) AddInstitutionWithAdmin(
        string name, string adminFullName, string adminUsername, string adminPassword)
    {
        var institution = NewInstitution(name);
        EnsureUsernameFree(adminUsername);
        var admin = new Admin
        {
            Username = adminUsername.Trim(),
            FullName = adminFullName,
            InstitutionId = institution.Id
        };
        admin.SetPassword(adminPassword);

        institutions.Add(institution);
        users.Add(admin);
        return (institution, admin);
    }

    public Branch AddBranch(string institutionId, string name, string address,
        TimeOnly opensAt, TimeOnly closesAt, IEnumerable<DayOfWeek> workingDays, IEnumerable<string> departments)
    {
        var institution = RequireInstitution(institutionId);
        if (string.IsNullOrWhiteSpace(name))
            throw new AppointmentException("Şube adı boş olamaz.");
        if (closesAt <= opensAt)
            throw new AppointmentException("Kapanış saati açılıştan sonra olmalı.");
        var days = workingDays.Distinct().ToList();
        if (days.Count == 0)
            throw new AppointmentException("En az bir çalışma günü seçilmeli.");

        var branch = new Branch
        {
            Name = name.Trim(),
            Address = address?.Trim() ?? "",
            OpensAt = opensAt,
            ClosesAt = closesAt,
            WorkingDays = days,
            Departments = departments
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(d => new Department(d))
                .ToList()
        };
        institution.Branches.Add(branch);
        institutions.Update(institution);
        return branch;
    }

    public void AddService(string institutionId, string branchId, MedicalService service)
    {
        var institution = RequireInstitution(institutionId);
        var branch = RequireBranch(institution, branchId);
        EnsureDepartment(branch, service.DepartmentName);
        if (branch.FindService(service.Id) is not null)
            throw new AppointmentException("Bu şubede aynı kimlikli bir hizmet zaten var.");
        branch.Services.Add(service);
        institutions.Update(institution);
    }

    public Doctor AddDoctor(string institutionId, string branchId, string username, string password,
        string fullName, string specialty, string departmentName, IEnumerable<string> serviceIds)
    {
        var branch = RequireBranch(RequireInstitution(institutionId), branchId);
        EnsureDepartment(branch, departmentName);
        EnsureUsernameFree(username);

        var doctor = new Doctor
        {
            Username = username.Trim(),
            FullName = fullName,
            Specialty = specialty?.Trim() ?? "",
            InstitutionId = institutionId,
            BranchId = branchId,
            DepartmentName = departmentName
        };
        foreach (var id in serviceIds)
        {
            if (branch.FindService(id) is null)
                throw new AppointmentException("Hizmet bu şubede tanımlı değil.");
            doctor.AddService(id);
        }
        doctor.SetPassword(password);
        users.Add(doctor);
        return doctor;
    }

    /// <summary>Registers health personnel (nurse, technician...) working at a branch.</summary>
    public Nurse AddNurse(string institutionId, string branchId, string username, string password,
        string fullName, string position, string departmentName)
    {
        var branch = RequireBranch(RequireInstitution(institutionId), branchId);
        EnsureDepartment(branch, departmentName);
        EnsureUsernameFree(username);

        var nurse = new Nurse
        {
            Username = username.Trim(),
            FullName = fullName,
            Position = string.IsNullOrWhiteSpace(position) ? "Hemşire" : position.Trim(),
            InstitutionId = institutionId,
            BranchId = branchId,
            DepartmentName = departmentName
        };
        nurse.SetPassword(password);
        users.Add(nurse);
        return nurse;
    }

    public Admin AddAdmin(string institutionId, string username, string password, string fullName, bool isSuperAdmin = false)
    {
        RequireInstitution(institutionId);
        EnsureUsernameFree(username);
        var admin = new Admin
        {
            Username = username.Trim(),
            FullName = fullName,
            InstitutionId = institutionId,
            IsSuperAdmin = isSuperAdmin
        };
        admin.SetPassword(password);
        users.Add(admin);
        return admin;
    }

    public IReadOnlyList<Doctor> DoctorsOf(string institutionId) =>
        users.GetAll().OfType<Doctor>().Where(d => d.InstitutionId == institutionId).ToList();

    public IReadOnlyList<Nurse> NursesOf(string institutionId) =>
        users.GetAll().OfType<Nurse>().Where(n => n.InstitutionId == institutionId).ToList();

    public IReadOnlyList<Doctor> DoctorsOffering(string institutionId, string branchId, string serviceId) =>
        DoctorsOf(institutionId).Where(d => d.BranchId == branchId && d.Offers(serviceId)).ToList();

    /// <summary>The branch a staff member works at (null if it no longer exists).</summary>
    public Branch? BranchOf(Staff staff) =>
        institutions.GetById(staff.InstitutionId)?.FindBranch(staff.BranchId);

    /// <summary>Lets the doctor offer one more service of their branch.</summary>
    public void AssignService(Doctor doctor, string serviceId)
    {
        var branch = BranchOf(doctor) ?? throw new AppointmentException("Doktorun şubesi bulunamadı.");
        if (branch.FindService(serviceId) is null)
            throw new AppointmentException("Hizmet doktorun şubesinde tanımlı değil.");
        if (doctor.Offers(serviceId))
            throw new AppointmentException("Doktor bu hizmeti zaten veriyor.");
        doctor.AddService(serviceId);
        users.Update(doctor);
    }

    /// <summary>Stops the doctor offering a service. Existing appointments are kept.</summary>
    public void UnassignService(Doctor doctor, string serviceId)
    {
        if (!doctor.RemoveService(serviceId))
            throw new AppointmentException("Doktor bu hizmeti zaten vermiyor.");
        users.Update(doctor);
    }

    /// <summary>Adds a weekly availability window; it must fit inside the branch's working days and hours.</summary>
    public void AddAvailability(Doctor doctor, AvailabilitySlot slot)
    {
        var branch = BranchOf(doctor) ?? throw new AppointmentException("Doktorun şubesi bulunamadı.");
        if (!branch.WorkingDays.Contains(slot.Day))
            throw new AppointmentException("Şube bu gün kapalı, müsaitlik eklenemez.");
        if (slot.Start < branch.OpensAt || slot.End > branch.ClosesAt)
            throw new AppointmentException(
                $"Müsaitlik şubenin çalışma saatleri ({branch.OpensAt:HH:mm}-{branch.ClosesAt:HH:mm}) içinde olmalı.");
        doctor.AddAvailability(slot);
        users.Update(doctor);
    }

    public void RemoveAvailability(Doctor doctor, int index)
    {
        if (!doctor.RemoveAvailability(index))
            throw new AppointmentException("Müsaitlik aralığı bulunamadı.");
        users.Update(doctor);
    }

    /// <summary>Persists changes made directly on a doctor (availability, services).</summary>
    public void SaveDoctor(Doctor doctor) => users.Update(doctor);

    private static Institution NewInstitution(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppointmentException("Kurum adı boş olamaz.");
        return new Institution { Name = name.Trim() };
    }

    private Institution RequireInstitution(string id) =>
        institutions.GetById(id) ?? throw new AppointmentException("Kurum bulunamadı.");

    private static Branch RequireBranch(Institution institution, string branchId) =>
        institution.FindBranch(branchId) ?? throw new AppointmentException("Şube bulunamadı.");

    private static void EnsureDepartment(Branch branch, string departmentName)
    {
        if (!string.IsNullOrEmpty(departmentName) && !branch.Departments.Any(d => d.Name == departmentName))
            throw new AppointmentException("Bölüm bu şubede tanımlı değil.");
    }

    private void EnsureUsernameFree(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            throw new AppointmentException("Kullanıcı adı en az 3 karakter olmalı.");
        if (users.GetAll().Any(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new AppointmentException("Bu kullanıcı adı zaten kullanılıyor.");
    }
}
