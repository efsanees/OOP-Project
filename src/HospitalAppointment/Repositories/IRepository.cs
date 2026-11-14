using HospitalAppointment.Models;

namespace HospitalAppointment.Repositories;

public interface IRepository<T> where T : IEntity
{
    IReadOnlyList<T> GetAll();
    T? GetById(string id);
    void Add(T item);
    /// <summary>Persists changes made to an already stored item.</summary>
    void Update(T item);
    bool Remove(string id);
}
