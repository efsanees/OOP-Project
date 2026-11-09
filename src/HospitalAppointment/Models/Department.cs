namespace HospitalAppointment.Models;

public class Department
{
    public string Name { get; init; } = "";

    public Department() { }
    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Bölüm adı boş olamaz.");
        Name = name.Trim();
    }

    public override string ToString() => Name;
}
