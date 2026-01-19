using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Project;

public class CreateProjectDto
{
    [Required(ErrorMessage = "Proje adı gereklidir")]
    [MaxLength(200, ErrorMessage = "Proje adı maksimum 200 karakter olabilir")]
    public string Name { get; set; } = default!;

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Captain kullanıcı ID'si zorunludur")]
    public Guid CaptainUserId { get; set; }
}