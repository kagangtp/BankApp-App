using IlkProjem.Core.Models;
using IlkProjem.DAL.Data;
using IlkProjem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IlkProjem.DAL.Repositories;

public class HouseRepository : IHouseRepository
{
    private readonly AppDbContext _context;

    public HouseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<House>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Houses
            .Where(h => h.CustomerId == customerId)
            .ToListAsync(ct);
    }

    public async Task<House?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Houses.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAsync(House house, CancellationToken ct = default)
    {
        await _context.Houses.AddAsync(house, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(House house, CancellationToken ct = default)
    {
        _context.Houses.Update(house);
        int affectedRows = await _context.SaveChangesAsync(ct);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var house = await _context.Houses.FindAsync(new object[] { id }, ct);
        if (house == null) return false;

        _context.Houses.Remove(house);
        int affectedRows = await _context.SaveChangesAsync(ct);
        return affectedRows > 0;
    }
}
