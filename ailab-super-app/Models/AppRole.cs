using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

public class AppRole : IdentityRole<Guid>
{
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Permissions { get; set; } // JSON string olarak saklanacak

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}