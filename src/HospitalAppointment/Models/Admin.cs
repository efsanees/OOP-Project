namespace HospitalAppointment.Models;

/// <summary>Institution manager. A super admin may additionally create institutions.</summary>
public class Admin : User
{
    public string InstitutionId { get; set; } = "";
    public bool IsSuperAdmin { get; set; }

    public override string RoleName => IsSuperAdmin ? "Sistem Yöneticisi" : "Hastane/Klinik Yöneticisi";
}
