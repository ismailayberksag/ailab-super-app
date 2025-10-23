using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Project;

public class AddProjectMemberDto
{
    [Required(ErrorMessage = "Kullanıcı ID gereklidir")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Rol gereklidir")]
    public string Role { get; set; } = "Member"; // Default: Member
}

