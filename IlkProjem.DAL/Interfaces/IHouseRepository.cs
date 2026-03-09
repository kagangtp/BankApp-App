using IlkProjem.Core.Models;

namespace IlkProjem.DAL.Interfaces;

public interface IHouseRepository
{
    Task<List<House>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<House?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(House house, CancellationToken ct = default);
    Task<bool> UpdateAsync(House house, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
