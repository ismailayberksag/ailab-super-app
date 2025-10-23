using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Project;

public class UpdateProjectMemberRoleDto
{
    [Required(ErrorMessage = "Rol gereklidir")]
    public string Role { get; set; } = default!;
}

