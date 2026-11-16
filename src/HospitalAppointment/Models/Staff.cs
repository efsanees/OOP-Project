namespace HospitalAppointment.Models;

/// <summary>Any employee that works at a branch.</summary>
public abstract class Staff : User
{
    public string InstitutionId { get; set; } = "";
    public string BranchId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
}

/// <summary>Health personnel other than doctors (nurse, lab technician, radiology technician...).</summary>
public class Nurse : Staff
{
    /// <summary>Job title, e.g. "Hemşire" or "Laborant".</summary>
    public string Position { get; set; } = "Hemşire";

    public override string RoleName => "Sağlık Personeli";

    public override string Describe() => $"{FullName} - {Position} ({DepartmentName})";
}
