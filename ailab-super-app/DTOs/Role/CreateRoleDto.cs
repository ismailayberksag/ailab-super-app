using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Role;

public class CreateRoleDto
{
    [Required(ErrorMessage = "Rol adı gereklidir")]
    [MaxLength(256, ErrorMessage = "Rol adı maksimum 256 karakter olabilir")]
    public string Name { get; set; } = default!;

    [MaxLength(500, ErrorMessage = "Açıklama maksimum 500 karakter olabilir")]
    public string? Description { get; set; }

    public List<string>? Permissions { get; set; }
}

