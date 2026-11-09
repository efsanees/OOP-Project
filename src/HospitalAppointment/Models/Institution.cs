namespace HospitalAppointment.Models;

/// <summary>A hospital or clinic group. A single clinic simply has one branch.</summary>
public class Institution : IEntity
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public List<Branch> Branches { get; set; } = [];

    public Branch? FindBranch(string branchId) => Branches.FirstOrDefault(b => b.Id == branchId);

    public override string ToString() => Name;
}
