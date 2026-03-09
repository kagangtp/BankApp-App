// Interfaces/ICustomerRepository.cs
using IlkProjem.Core.Models;
using IlkProjem.Core.Specifications;

namespace IlkProjem.DAL.Interfaces;
public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    // NEW: Accept a Specification and return only the matching customers
    Task<List<Customer>> ListAsync(ISpecification<Customer> spec, CancellationToken ct = default);
}