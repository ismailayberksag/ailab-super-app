using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

[Table("user_avatars")]
public class UserAvatar
{
    [Key]
    [ForeignKey("User")]
    public Guid UserId { get; set; }

    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = "image/png"; // png, jpeg vs.

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public virtual User User { get; set; } = null!;
}
