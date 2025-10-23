using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Project;

public class UpdateProjectDto
{
    [MaxLength(200, ErrorMessage = "Proje adı maksimum 200 karakter olabilir")]
    public string? Name { get; set; }

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }
}

