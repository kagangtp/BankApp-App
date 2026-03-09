using IlkProjem.Core.Models;
using IlkProjem.DAL.Data;
using IlkProjem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IlkProjem.DAL.Repositories;

public class CarRepository : ICarRepository
{
    private readonly AppDbContext _context;

    public CarRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Car>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Cars
            .Where(c => c.CustomerId == customerId)
            .ToListAsync(ct);
    }

    public async Task<Car?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Cars.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAsync(Car car, CancellationToken ct = default)
    {
        await _context.Cars.AddAsync(car, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Car car, CancellationToken ct = default)
    {
        _context.Cars.Update(car);
        int affectedRows = await _context.SaveChangesAsync(ct);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var car = await _context.Cars.FindAsync(new object[] { id }, ct);
        if (car == null) return false;

        _context.Cars.Remove(car);
        int affectedRows = await _context.SaveChangesAsync(ct);
        return affectedRows > 0;
    }
}
