using HospitalAppointment.Models;
using HospitalAppointment.Repositories;
using HospitalAppointment.Services;

namespace HospitalAppointment.UI;

/// <summary>Everything the menus need, passed around instead of using globals.</summary>
public record AppServices(
    IRepository<User> Users,
    IRepository<Institution> Institutions,
    IRepository<Appointment> Appointments,
    IAuthService Auth,
    CatalogService Catalog,
    AppointmentService Booking);

/// <summary>A role-specific menu. Each role provides its own implementation (polymorphism).</summary>
public interface IMenu
{
    void Run();
}

public static class MenuFactory
{
    public static IMenu? For(User user, AppServices services) => user switch
    {
        Patient p => new PatientMenu(p, services),
        Doctor d => new DoctorMenu(d, services),
        Nurse n => new NurseMenu(n, services),
        Admin a => new AdminMenu(a, services),
        _ => null
    };
}
