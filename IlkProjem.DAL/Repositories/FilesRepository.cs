using IlkProjem.Core.Models;
using IlkProjem.DAL.Data;
using Microsoft.EntityFrameworkCore;
using IlkProjem.DAL.Interfaces;

namespace IlkProjem.DAL.Repositories;

public class FilesRepository : IFilesRepository
{
    private readonly AppDbContext _context;

    public FilesRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Files file)
    {
        await _context.Files.AddAsync(file);
    }

    public async Task<Files?> GetByIdAsync(Guid id)
    {
        return await _context.Files.FindAsync(id);
    }

    public async Task<List<Files>> GetAllAsync()
    {
        return await _context.Files.ToListAsync();
    }

    public async Task<List<Files>> GetByOwnerAsync(string ownerType, int ownerId)
    {
        return await _context.Files
            .Where(f => f.OwnerType == ownerType && f.OwnerId == ownerId)
            .ToListAsync();
    }

    public void Update(Files file)
    {
        _context.Files.Update(file);
    }

    public void Delete(Files file)
    {
        _context.Files.Remove(file);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Files?> GetByHashAsync(string hash)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.FileHash == hash);
    }

    public async Task<int> CountByPathAsync(string relativePath)
    {
        // Bu yolu (path) kullanan kaç tane kayıt olduğunu sayar
        return await _context.Files.CountAsync(f => f.RelativePath == relativePath);
    }

}