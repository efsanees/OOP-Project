using System.Text;
using HospitalAppointment.Models;
using HospitalAppointment.Repositories;
using HospitalAppointment.Services;
using HospitalAppointment.UI;

Console.OutputEncoding = Encoding.UTF8;
// Reading UTF-8 from a Windows console turns Turkish letters (ş, ğ, ı, İ...) into '\0'.
// With UTF-16, .NET reads through the console's Unicode API instead. Piped input stays UTF-8.
Console.InputEncoding = OperatingSystem.IsWindows() && !Console.IsInputRedirected ? Encoding.Unicode : Encoding.UTF8;

// Data folder: first argument, otherwise ./data next to where the program is started.
var dataDir = args.Length > 0 ? args[0] : "data";
IRepository<User> users = new JsonRepository<User>(Path.Combine(dataDir, "users.json"));
IRepository<Institution> institutions = new JsonRepository<Institution>(Path.Combine(dataDir, "institutions.json"));
IRepository<Appointment> appointments = new JsonRepository<Appointment>(Path.Combine(dataDir, "appointments.json"));

var catalog = new CatalogService(institutions, users);
SeedData.EnsureSeeded(institutions, users, catalog);

var app = new AppServices(users, institutions, appointments,
    new AuthService(users), catalog, new AppointmentService(appointments, users, institutions));

try
{
    while (true)
    {
        ConsoleUi.Title("Hastane / Klinik Randevu Sistemi");
        Console.WriteLine("1) Giriş yap");
        Console.WriteLine("2) Hasta olarak kayıt ol");
        Console.WriteLine("0) Çıkış");
        switch (ConsoleUi.ReadInt("Seçiminiz", 0, 2))
        {
            case 1:
                var user = app.Auth.Login(ConsoleUi.ReadString("Kullanıcı adı"), ConsoleUi.ReadString("Şifre"));
                if (user is null) { Console.WriteLine("Kullanıcı adı veya şifre hatalı."); break; }
                var menu = MenuFactory.For(user, app);
                if (menu is null) { Console.WriteLine("Bu rol için tanımlı bir menü yok."); break; }
                Console.WriteLine($"Hoş geldiniz, {user.Describe()}");
                menu.Run();
                break;
            case 2:
                try
                {
                    app.Auth.RegisterPatient(
                        ConsoleUi.ReadString("Kullanıcı adı"), ConsoleUi.ReadString("Şifre (en az 4 karakter)"),
                        ConsoleUi.ReadString("Ad soyad"), ConsoleUi.ReadString("Telefon", allowEmpty: true));
                    Console.WriteLine("Kayıt tamamlandı, şimdi giriş yapabilirsiniz.");
                }
                catch (Exception ex) when (ex is AppointmentException or ArgumentException)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
                break;
            case 0:
                return;
        }
    }
}
catch (InputClosedException)
{
    // Every change is already saved to JSON, so it is safe to stop here.
    Console.WriteLine();
    Console.WriteLine("Girdi sonlandı, program kapatılıyor.");
}
