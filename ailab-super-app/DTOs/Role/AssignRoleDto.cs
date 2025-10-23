using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Role;

public class AssignRoleDto
{
    [Required(ErrorMessage = "Kullanıcı ID gereklidir")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Rol adı gereklidir")]
    public string RoleName { get; set; } = default!;
}

