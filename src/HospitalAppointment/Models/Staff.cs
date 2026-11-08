namespace HospitalAppointment.Models;

/// <summary>Any employee that works at a branch.</summary>
public abstract class Staff : User
{
    public string InstitutionId { get; set; } = "";
    public string BranchId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
}

public class Nurse : Staff
{
    public override string RoleName => "Sağlık Personeli";
}
