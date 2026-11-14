using HospitalAppointment.Models;

namespace HospitalAppointment.UI;

public class AdminMenu(Admin admin, AppServices app) : MenuBase(app)
{
    protected override string Title => $"{admin.RoleName} Menüsü - {admin.FullName}";

    protected override IReadOnlyList<(string Label, Action Action)> Options()
    {
        List<(string Label, Action Action)> options =
        [
            ("Kurum bilgilerini göster", ShowInstitution),
            ("Şube ekle", AddBranch),
            ("Hizmet ekle", AddService),
            ("Doktor ekle", AddDoctor),
            ("Sağlık personeli ekle", AddNurse),
            ("Doktorun hizmetlerini düzenle", EditDoctorServices),
            ("Doktor müsaitliği ekle", AddAvailability),
            ("Randevuları yönet (onay / red / iptal)", ManageAppointments)
        ];
        if (admin.IsSuperAdmin) options.Add(("Yeni kurum ve yönetici ekle", AddInstitution));
        return options;
    }

    /// <summary>Institution this admin is working on (super admin picks one).</summary>
    private Institution? CurrentInstitution()
    {
        if (!admin.IsSuperAdmin) return App.Catalog.GetInstitution(admin.InstitutionId);
        return ConsoleUi.Choose("Kurum seçin", App.Catalog.Institutions, i => i.Name);
    }

    private void ShowInstitution()
    {
        var inst = CurrentInstitution();
        if (inst is null) return;
        Console.WriteLine($"Kurum: {inst.Name}");
        foreach (var b in inst.Branches)
        {
            var days = string.Join(", ", b.WorkingDays.Select(ConsoleUi.DayName));
            Console.WriteLine($"- Şube: {b} | Günler: {days} | Adres: {b.Address}");
            Console.WriteLine($"    Bölümler: {string.Join(", ", b.Departments)}");
            foreach (var s in b.Services) Console.WriteLine($"    Hizmet: {s}");
        }
        foreach (var d in App.Catalog.DoctorsOf(inst.Id))
        {
            var services = string.Join(", ", inst.FindBranch(d.BranchId)?.Services
                .Where(s => d.Offers(s.Id)).Select(s => s.Name) ?? []);
            Console.WriteLine($"- {d.Describe()} (şube: {inst.FindBranch(d.BranchId)?.Name}) | Hizmetler: {services}");
        }
        foreach (var n in App.Catalog.NursesOf(inst.Id))
            Console.WriteLine($"- {n.Describe()} (şube: {inst.FindBranch(n.BranchId)?.Name})");
    }

    private void AddBranch()
    {
        var inst = CurrentInstitution();
        if (inst is null) return;
        var name = ConsoleUi.ReadString("Şube adı");
        var address = ConsoleUi.ReadString("Adres", allowEmpty: true);
        var opens = ConsoleUi.ReadTime("Açılış saati");
        var closes = ConsoleUi.ReadTime("Kapanış saati");
        Console.WriteLine("Çalışma günleri: 5 = Pazartesi-Cuma, 6 = Pazartesi-Cumartesi, 7 = her gün");
        var count = ConsoleUi.ReadInt("Seçiminiz", 5, 7);
        DayOfWeek[] all = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        var departments = ConsoleUi.ReadString("Bölümler (virgülle ayırın)").Split(',', StringSplitOptions.TrimEntries);
        App.Catalog.AddBranch(inst.Id, name, address, opens, closes, all.Take(count), departments);
        Console.WriteLine("Şube eklendi.");
    }

    private void AddService()
    {
        var inst = CurrentInstitution();
        var branch = inst is null ? null : ConsoleUi.Choose("Şube seçin", inst.Branches, b => b.Name);
        if (inst is null || branch is null) return;
        var department = ConsoleUi.Choose("Hizmeti veren bölüm", branch.Departments, d => d.Name);
        if (department is null) return;

        Console.WriteLine("Hizmet türü: 1) Muayene  2) Radyoloji  3) Laboratuvar");
        MedicalService service = ConsoleUi.ReadInt("Tür", 1, 3) switch
        {
            1 => new Examination(),
            2 => new Radiology(),
            _ => new Laboratory()
        };
        service.Name = ConsoleUi.ReadString("Hizmet adı");
        service.DurationMinutes = ConsoleUi.ReadInt("Süre (dk)", 5, 240);
        service.BasePrice = ConsoleUi.ReadDecimal("Temel ücret (TL)");
        service.DepartmentName = department.Name;
        App.Catalog.AddService(inst.Id, branch.Id, service);
        Console.WriteLine($"Hizmet eklendi, hasta ücreti: {service.CalculatePrice():N2} TL");
        Console.WriteLine("Not: Hizmetin randevuya açılması için 'Doktorun hizmetlerini düzenle' ile bir doktora atayın.");
    }

    private void AddDoctor()
    {
        var inst = CurrentInstitution();
        var branch = inst is null ? null : ConsoleUi.Choose("Şube seçin", inst.Branches, b => b.Name);
        if (inst is null || branch is null) return;
        var department = ConsoleUi.Choose("Bölüm", branch.Departments, d => d.Name);
        if (department is null) return;

        var fullName = ConsoleUi.ReadString("Ad soyad");
        var specialty = ConsoleUi.ReadString("Uzmanlık alanı");
        var username = ConsoleUi.ReadString("Kullanıcı adı");
        var password = ConsoleUi.ReadString("Şifre (en az 4 karakter)");

        var serviceIds = new List<string>();
        while (true)
        {
            var remaining = branch.Services.Where(s => !serviceIds.Contains(s.Id)).ToList();
            if (remaining.Count == 0) break;
            var service = ConsoleUi.Choose("Doktorun vereceği hizmeti ekleyin (0 = bitti)", remaining, s => s.ToString());
            if (service is null) break;
            serviceIds.Add(service.Id);
        }

        App.Catalog.AddDoctor(inst.Id, branch.Id, username, password, fullName, specialty, department.Name, serviceIds);
        Console.WriteLine("Doktor eklendi. Randevu alınabilmesi için müsaitlik ekleyin.");
    }

    private void AddNurse()
    {
        var inst = CurrentInstitution();
        var branch = inst is null ? null : ConsoleUi.Choose("Şube seçin", inst.Branches, b => b.Name);
        if (inst is null || branch is null) return;
        var department = ConsoleUi.Choose("Bölüm", branch.Departments, d => d.Name);
        if (department is null) return;

        var fullName = ConsoleUi.ReadString("Ad soyad");
        var position = ConsoleUi.ReadString("Görevi (ör. Hemşire, Laborant, Radyoloji teknisyeni)");
        var username = ConsoleUi.ReadString("Kullanıcı adı");
        var password = ConsoleUi.ReadString("Şifre (en az 4 karakter)");
        App.Catalog.AddNurse(inst.Id, branch.Id, username, password, fullName, position, department.Name);
        Console.WriteLine("Sağlık personeli eklendi.");
    }

    private void EditDoctorServices()
    {
        var inst = CurrentInstitution();
        if (inst is null) return;
        var doctor = ConsoleUi.Choose("Doktor seçin", App.Catalog.DoctorsOf(inst.Id), d => d.Describe());
        if (doctor is null) return;
        var branch = App.Catalog.BranchOf(doctor);
        if (branch is null) { Console.WriteLine("Doktorun şubesi bulunamadı."); return; }

        var offered = branch.Services.Where(s => doctor.Offers(s.Id)).ToList();
        Console.WriteLine("Doktorun verdiği hizmetler:");
        if (offered.Count == 0) Console.WriteLine("  (yok)");
        foreach (var s in offered) Console.WriteLine($"  - {s}");

        Console.WriteLine("1) Hizmet ekle   2) Hizmet çıkar   0) Vazgeç");
        switch (ConsoleUi.ReadInt("Seçiminiz", 0, 2))
        {
            case 1:
                var toAdd = ConsoleUi.Choose("Eklenecek hizmet",
                    branch.Services.Where(s => !doctor.Offers(s.Id)).ToList(), s => s.ToString());
                if (toAdd is null) return;
                App.Catalog.AssignService(doctor, toAdd.Id);
                Console.WriteLine("Hizmet doktora eklendi.");
                break;
            case 2:
                var toRemove = ConsoleUi.Choose("Çıkarılacak hizmet", offered, s => s.ToString());
                if (toRemove is null) return;
                App.Catalog.UnassignService(doctor, toRemove.Id);
                Console.WriteLine("Hizmet doktordan çıkarıldı (mevcut randevular korunur).");
                break;
        }
    }

    private void AddAvailability()
    {
        var inst = CurrentInstitution();
        if (inst is null) return;
        var doctor = ConsoleUi.Choose("Doktor seçin", App.Catalog.DoctorsOf(inst.Id), d => d.Describe());
        if (doctor is null) return;
        Console.WriteLine("Mevcut müsaitlik:");
        ConsoleUi.PrintAvailability(doctor);
        App.Catalog.AddAvailability(doctor, ConsoleUi.ReadAvailabilitySlot());
        Console.WriteLine("Müsaitlik eklendi.");
    }

    private void ManageAppointments()
    {
        var inst = CurrentInstitution();
        if (inst is null) return;
        var open = App.Booking.ForInstitution(inst.Id).Where(a => a.IsActive).ToList();
        var selected = ConsoleUi.Choose("Aktif randevular", open, a => a.ToString());
        if (selected is null) return;
        Console.WriteLine("1) Onayla   2) Reddet   3) İptal et   0) Vazgeç");
        switch (ConsoleUi.ReadInt("Seçiminiz", 0, 3))
        {
            case 1: App.Booking.Confirm(selected.Id, admin); Console.WriteLine("Onaylandı."); break;
            case 2: App.Booking.Reject(selected.Id, admin); Console.WriteLine("Reddedildi."); break;
            case 3: App.Booking.Cancel(selected.Id, admin); Console.WriteLine("İptal edildi."); break;
        }
    }

    private void AddInstitution()
    {
        var name = ConsoleUi.ReadString("Kurum adı");
        var fullName = ConsoleUi.ReadString("Kurum yöneticisi ad soyad");
        var username = ConsoleUi.ReadString("Yönetici kullanıcı adı");
        var password = ConsoleUi.ReadString("Yönetici şifresi (en az 4 karakter)");
        App.Catalog.AddInstitutionWithAdmin(name, fullName, username, password);
        Console.WriteLine("Kurum ve yönetici eklendi. Şubeleri yeni yönetici ekleyebilir.");
    }
}
