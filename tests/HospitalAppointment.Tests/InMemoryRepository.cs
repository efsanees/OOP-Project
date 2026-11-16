using HospitalAppointment.Models;
using HospitalAppointment.Repositories;

namespace HospitalAppointment.Tests;

public class InMemoryRepository<T> : IRepository<T> where T : IEntity
{
    private readonly List<T> _items = [];

    public IReadOnlyList<T> GetAll() => _items.ToList();
    public T? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
    public void Add(T item) => _items.Add(item);

    public void Update(T item)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        if (index < 0) throw new KeyNotFoundException(item.Id);
        _items[index] = item;
    }

    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;
}
