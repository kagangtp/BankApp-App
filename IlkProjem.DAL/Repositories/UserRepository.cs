using IlkProjem.Core.Models;
using IlkProjem.DAL.Data;
using IlkProjem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace IlkProjem.DAL.Repositories;
public class UserRepository : IUserRepository

{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // PostgreSQL'de asenkron ve güvenli arama
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) => 
        await _context.Users.AnyAsync(u => u.Email == email, ct); // Token buraya geçti

    public async Task AddAsync(User user, CancellationToken ct = default) => 
        await _context.Users.AddAsync(user, ct);

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default) => 
        await _context.SaveChangesAsync(ct) > 0;
}
