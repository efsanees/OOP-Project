using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace HospitalAppointment.Models;

/// <summary>Base class of every account. Passwords are hashed (PBKDF2) and never exposed.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$role")]
[JsonDerivedType(typeof(Patient), "patient")]
[JsonDerivedType(typeof(Doctor), "doctor")]
[JsonDerivedType(typeof(Nurse), "nurse")]
[JsonDerivedType(typeof(Admin), "admin")]
public abstract class User : IEntity
{
    private string _fullName = "";

    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Username { get; init; } = "";

    public string FullName
    {
        get => _fullName;
        set => _fullName = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Ad soyad boş olamaz.")
            : value.Trim();
    }

    [JsonInclude] public string PasswordHash { get; private set; } = "";
    [JsonInclude] public string Salt { get; private set; } = "";

    /// <summary>Role label shown in the UI; each subclass supplies its own.</summary>
    [JsonIgnore]
    public abstract string RoleName { get; }

    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            throw new ArgumentException("Şifre en az 4 karakter olmalı.");
        var salt = RandomNumberGenerator.GetBytes(16);
        Salt = Convert.ToBase64String(salt);
        PasswordHash = Hash(password, salt);
    }

    public bool VerifyPassword(string password) =>
        PasswordHash.Length > 0 &&
        CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(PasswordHash),
            Convert.FromBase64String(Hash(password, Convert.FromBase64String(Salt))));

    private static string Hash(string password, byte[] salt) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 50_000, HashAlgorithmName.SHA256, 32));

    public virtual string Describe() => $"{FullName} ({RoleName})";
}
