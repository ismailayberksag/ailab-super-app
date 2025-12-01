using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

[Table("avatars")]
public class Avatar
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty; // Firebase içindeki path (örn: avatars/man01.png)

    [Required]
    [MaxLength(1000)]
    public string PublicUrl { get; set; } = string.Empty; // Erişim linki

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Man01", "Default", "Kullanıcı Yüklemesi"

    public bool IsSystemDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
