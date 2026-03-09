using IlkProjem.Core.Models;

namespace IlkProjem.DAL.Interfaces;

public interface ICarRepository
{
    Task<List<Car>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Car?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Car car, CancellationToken ct = default);
    Task<bool> UpdateAsync(Car car, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
