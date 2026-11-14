using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Services;

public interface IAuthService
{
    User? Login(string username, string password);
    Patient RegisterPatient(string username, string password, string fullName, string phone);
}

public class AuthService(IRepository<User> users) : IAuthService
{
    public User? Login(string username, string password)
    {
        var user = users.GetAll().FirstOrDefault(u =>
            string.Equals(u.Username, username?.Trim(), StringComparison.OrdinalIgnoreCase));
        return user is not null && user.VerifyPassword(password ?? "") ? user : null;
    }

    public Patient RegisterPatient(string username, string password, string fullName, string phone)
    {
        username = username?.Trim() ?? "";
        if (username.Length < 3)
            throw new AppointmentException("Kullanıcı adı en az 3 karakter olmalı.");
        if (IsUsernameTaken(username))
            throw new AppointmentException("Bu kullanıcı adı zaten kullanılıyor.");

        var patient = new Patient { Username = username, FullName = fullName, Phone = phone ?? "" };
        patient.SetPassword(password);
        users.Add(patient);
        return patient;
    }

    public bool IsUsernameTaken(string username) =>
        users.GetAll().Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
}
