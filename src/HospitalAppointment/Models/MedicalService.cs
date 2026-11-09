using System.Text.Json.Serialization;

namespace HospitalAppointment.Models;

/// <summary>
/// A service a branch offers (examination, radiology, laboratory...).
/// Subclasses override <see cref="CalculatePrice"/> / <see cref="Category"/> (polymorphism).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Examination), "examination")]
[JsonDerivedType(typeof(Radiology), "radiology")]
[JsonDerivedType(typeof(Laboratory), "laboratory")]
public abstract class MedicalService
{
    private string _name = "";
    private int _durationMinutes = 15;
    private decimal _basePrice;

    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Hizmet adı boş olamaz.")
            : value.Trim();
    }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => _durationMinutes = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Süre 0'dan büyük olmalı.");
    }

    public decimal BasePrice
    {
        get => _basePrice;
        set => _basePrice = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Ücret negatif olamaz.");
    }

    /// <summary>Department (by name) that provides the service.</summary>
    public string DepartmentName { get; set; } = "";

    [JsonIgnore]
    public abstract string Category { get; }

    /// <summary>Final price patient pays. Each service type computes it differently.</summary>
    public abstract decimal CalculatePrice();

    public override string ToString() =>
        $"{Name} [{Category}] - {DurationMinutes} dk - {CalculatePrice():N2} TL";
}

public class Examination : MedicalService
{
    public override string Category => "Muayene";
    public override decimal CalculatePrice() => BasePrice;
}

public class Radiology : MedicalService
{
    /// <summary>Device / film surcharge as a ratio of the base price.</summary>
    public decimal DeviceFeeRate { get; set; } = 0.20m;

    public override string Category => "Radyoloji";
    public override decimal CalculatePrice() => Math.Round(BasePrice * (1 + DeviceFeeRate), 2);
}

public class Laboratory : MedicalService
{
    /// <summary>Fixed sample-collection fee.</summary>
    public decimal SampleFee { get; set; } = 25m;

    public override string Category => "Laboratuvar";
    public override decimal CalculatePrice() => BasePrice + SampleFee;
}
