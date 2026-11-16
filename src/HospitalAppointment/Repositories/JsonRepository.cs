using System.Text.Json;
using System.Text.Json.Serialization;
using HospitalAppointment.Models;

namespace HospitalAppointment.Repositories;

/// <summary>Keeps all items in memory and writes them to a JSON file after every change.</summary>
public class JsonRepository<T> : IRepository<T> where T : IEntity
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly List<T> _items;

    public JsonRepository(string path)
    {
        _path = path;
        _items = File.Exists(path)
            ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Options) ?? []
            : [];
    }

    public IReadOnlyList<T> GetAll() => _items.ToList();

    public T? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);

    public void Add(T item)
    {
        if (_items.Any(i => i.Id == item.Id))
            throw new InvalidOperationException($"Aynı kimlikli kayıt zaten var: {item.Id}");
        _items.Add(item);
        Save();
    }

    public void Update(T item)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        if (index < 0) throw new KeyNotFoundException($"Kayıt bulunamadı: {item.Id}");
        _items[index] = item;
        Save();
    }

    public bool Remove(string id)
    {
        var removed = _items.RemoveAll(i => i.Id == id) > 0;
        if (removed) Save();
        return removed;
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(_items, Options));
    }
}
