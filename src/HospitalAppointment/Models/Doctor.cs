namespace HospitalAppointment.Models;

public class Doctor : Staff
{
    private readonly List<AvailabilitySlot> _availability = [];
    private readonly List<string> _serviceIds = [];

    public string Specialty { get; set; } = "";

    // Collections are exposed read-only; changes go through the methods below (encapsulation).
    public IReadOnlyList<AvailabilitySlot> Availability
    {
        get => _availability;
        init => _availability = [.. value];
    }

    public IReadOnlyList<string> ServiceIds
    {
        get => _serviceIds;
        init => _serviceIds = [.. value];
    }

    public override string RoleName => "Doktor";

    public override string Describe() => $"Dr. {FullName} - {Specialty}";

    public bool Offers(string serviceId) => _serviceIds.Contains(serviceId);

    public void AddService(string serviceId)
    {
        if (!_serviceIds.Contains(serviceId)) _serviceIds.Add(serviceId);
    }

    public bool RemoveService(string serviceId) => _serviceIds.Remove(serviceId);

    public void AddAvailability(AvailabilitySlot slot)
    {
        if (_availability.Any(s => s.Overlaps(slot)))
            throw new InvalidOperationException("Bu aralık mevcut bir müsaitlik aralığıyla çakışıyor.");
        _availability.Add(slot);
    }

    public bool RemoveAvailability(int index)
    {
        if (index < 0 || index >= _availability.Count) return false;
        _availability.RemoveAt(index);
        return true;
    }

    /// <summary>True if [start, end) falls entirely inside one of the doctor's availability slots.</summary>
    public bool IsAvailable(DateTime start, DateTime end) =>
        _availability.Any(s => s.Contains(start, end));
}
