using Microsoft.AspNetCore.Identity;

namespace ailab_super_app.Models;

public class UserRole : IdentityUserRole<Guid>
{
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual AppRole Role { get; set; } = null!;
}