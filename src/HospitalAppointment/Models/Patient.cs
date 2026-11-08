namespace HospitalAppointment.Models;

public class Patient : User
{
    public string Phone { get; set; } = "";
    public override string RoleName => "Hasta";
}
