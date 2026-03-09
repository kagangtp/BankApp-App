using IlkProjem.Core.Models;

namespace IlkProjem.DAL.Interfaces;

public interface IUserRepository
{
  // Giriş için kullanıcıyı e-postasıyla bulur
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<bool> SaveChangesAsync(CancellationToken ct = default);
}