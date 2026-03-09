using IlkProjem.Core.Models;

namespace IlkProjem.DAL.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Hash'lenmiş token değerine göre refresh token'ı bulur
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string hashedToken, CancellationToken ct = default);
    
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);
    
    Task<bool> SaveChangesAsync(CancellationToken ct = default);
}
