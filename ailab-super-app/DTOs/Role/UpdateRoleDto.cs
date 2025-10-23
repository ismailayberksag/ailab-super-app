using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Role;

public class UpdateRoleDto
{
    [MaxLength(500, ErrorMessage = "Açıklama maksimum 500 karakter olabilir")]
    public string? Description { get; set; }

    public List<string>? Permissions { get; set; }
}

