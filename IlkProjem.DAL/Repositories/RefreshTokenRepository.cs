using IlkProjem.Core.Models;
using IlkProjem.DAL.Data;
using IlkProjem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IlkProjem.DAL.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context) => _context = context;

    public async Task<RefreshToken?> GetByTokenAsync(string hashedToken, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await _context.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct) > 0;
}
