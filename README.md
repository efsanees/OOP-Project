# Hastane / Klinik Randevu Sistemi

Nesneye Dayalı Programlama dersi ödevi (Seçenek A). C# (.NET 10) konsol uygulaması; veriler JSON dosyalarında saklanır.

## Çalıştırma

```
dotnet run --project src/HospitalAppointment            # veriler ./data klasörüne yazılır
dotnet run --project src/HospitalAppointment -- <klasör> # farklı bir veri klasörü
dotnet test                                              # birim testleri
```

İlk çalıştırmada örnek veri oluşturulur (kullanıcı adı / şifre):

| Rol | Kullanıcı | Şifre |
|---|---|---|
| Hasta | `hasta` | `1234` |
| Doktor (Merkez şube) | `ayse` | `1234` |
| Doktor (Serdivan şube) | `mehmet` | `1234` |
| Sağlık personeli / hemşire (Merkez şube) | `zehra` | `1234` |
| Kurum yöneticisi | `admin` | `1234` |
| Sistem yöneticisi (yeni kurum ekleyebilir) | `super` | `1234` |

Sıfırlamak için `data` klasörünü silin (örnek veri yalnızca `data` boşken oluşturulur; eski bir `data` klasöründe `zehra` kullanıcısı yoktur).

## Özellikler (ödev gereksinimleri)

- **Kurum / şube:** `Institution` → `Branch` → `Department`. Tek klinik de, çok şubeli kuruluş da tanımlanabilir. Her şubenin çalışma saatleri ve günleri vardır.
- **Hizmetler:** `MedicalService` (ad, süre, ücret, bölüm); Muayene, Radyoloji, Laboratuvar türleri.
- **Çalışanlar:** Yönetici doktor ve sağlık personeli (hemşire, laborant, teknisyen…) kaydeder.
  - Doktor: uzmanlık alanı, verdiği hizmetler (sonradan eklenip çıkarılabilir), haftalık müsaitlik aralıkları. Müsaitlik şubenin çalışma gün/saatleri içinde olmak zorundadır.
  - Sağlık personeli: görevi, şubesi ve bölümü; kendi şubesinin randevu listesini (günlük ya da tümü) görür ve bekleyen randevuları onaylar/reddeder.
- **Randevu:** Hasta kurum → şube → hizmet → doktor → tarih seçer; yalnızca doktorun müsait ve boş saatleri listelenir.
  - Kurallar: geçmişe randevu yok, şube açık olmalı, doktor müsait olmalı, **doktor çakışması** ve hasta çakışması engellenir.
  - Kayıt: işlem, süre, ücret, çalışan, tarih/saat, durum.
  - **Onay:** `Pending` → doktor, şubenin sağlık personeli veya yönetici `Confirmed` / `Rejected` yapar; hasta `Cancelled` yapabilir. İptal/red edilen randevu saati tekrar boşa çıkar.
- **Roller:** Hasta, Doktor, Sağlık personeli, Kurum yöneticisi (+ Sistem yöneticisi). Her rolün kendi menüsü ve yetkisi vardır.

## OOP ilkelerinin kullanımı

| İlke | Nerede |
|---|---|
| Kalıtım | `User` → `Patient`, `Admin`, `Staff` → `Doctor`, `Nurse`; `MedicalService` → `Examination`, `Radiology`, `Laboratory` |
| Çok biçimlilik | `MedicalService.CalculatePrice()` (muayene: temel ücret, radyoloji: +%20 cihaz payı, laboratuvar: +25 TL numune), `User.RoleName`/`Describe()`, `IMenu` / `MenuBase.Options()` (her rol kendi menüsü), JSON'da çok biçimli (de)serileştirme |
| Kapsülleme | Doğrulamalı property'ler (`Name`, `DurationMinutes`, `BasePrice`); `Appointment.Status` yalnızca `Confirm/Reject/Cancel` ile değişir; `Doctor` listeleri salt okunur açılır, `AddAvailability` çakışmayı denetler; şifreler PBKDF2 ile hash'lenir |
| Soyutlama / arayüz | `IRepository<T>`, `IAuthService`, `IMenu`, soyut `User`, `Staff`, `MedicalService`, `MenuBase` |
| Tasarım desenleri | Template Method: `MenuBase.Run()` ortak menü döngüsü + hata yönetimi, alt sınıflar yalnızca seçeneklerini verir. Factory: `MenuFactory.For(user)`. Repository: `IRepository<T>` |
| SOLID | SRP: model / servis / depo / arayüz katmanları ayrı. DIP: servisler `IRepository<T>`'ye bağımlı (testlerde `InMemoryRepository` kullanılır). OCP: yeni hizmet türü eklemek mevcut kodu değiştirmez |

## Sınıf yapısı

```
Models        Institution 1─* Branch 1─* Department
                               Branch 1─* MedicalService (Examination | Radiology | Laboratory)
              User (abstract) ─ Patient
                              ─ Admin
                              ─ Staff (abstract) ─ Doctor (AvailabilitySlot*, ServiceIds*)
                                                 ─ Nurse (sağlık personeli: Position)
              Appointment (Pending / Confirmed / Rejected / Cancelled)
Repositories  IRepository<T> ◄─ JsonRepository<T>
Services      AppointmentService (rezervasyon, çakışma, onay)  CatalogService (yönetim)
              AuthService : IAuthService                        SeedData
UI            ConsoleUi, IMenu ◄─ MenuBase (abstract) ◄─ PatientMenu | DoctorMenu | NurseMenu | AdminMenu
              MenuFactory
```

## Klasör yapısı

```
src/HospitalAppointment/   uygulama (Models, Repositories, Services, UI, Program.cs)
tests/HospitalAppointment.Tests/   xUnit testleri (çakışma, müsaitlik, onay akışı, sağlık personeli, yönetim, JSON)
```
