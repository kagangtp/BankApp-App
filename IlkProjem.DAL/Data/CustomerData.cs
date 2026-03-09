using Microsoft.EntityFrameworkCore;
using IlkProjem.Core.Models;
using IlkProjem.Core.Interfaces; // ICurrentUserService burada varsayıyoruz

namespace IlkProjem.DAL.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    // ICurrentUserService'i içeriye enjekte ediyoruz
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) 
        : base(options) 
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Files> Files => Set<Files>();
    public DbSet<House> Houses => Set<House>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<ServiceLog> ServiceLog => Set<ServiceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. GLOBAL QUERY FILTER (Tüm tablolarda IsDeleted = false olanları getir)
        // Eğer modellerin BaseEntity'den türüyorsa bu filtre otomatik uygulanır
        modelBuilder.Entity<Customer>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<House>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<Car>().HasQueryFilter(m => !m.IsDeleted);

        // 2. Files Tablosu Yapılandırması
        modelBuilder.Entity<Files>(entity =>
        {
            entity.ToTable("Files");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.CreatedAt); 
            entity.HasIndex(e => e.RelativePath);
            entity.HasIndex(e => e.FileHash);
        });

        // 3. Customer Tablosu
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
        });

        // 4. RefreshToken Yapılandırması
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.Token).IsUnique();
        });
    }
}