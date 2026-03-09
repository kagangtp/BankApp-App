using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IlkProjem.Core.Models;

[Table("Files")]
public class Files
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public string RelativePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FileHash { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; } 

    // --- YENİ POLİMORFİK YAPI ---
    
    // Hangi varlığın ID'si? (Evin ID'si 10 ise burası 10 olur)
    public int? OwnerId { get; set; } 

    // Bu dosya kime ait? ("House", "Car", "Customer" vb.)
    [MaxLength(50)]
    public string? OwnerType { get; set; } 
}