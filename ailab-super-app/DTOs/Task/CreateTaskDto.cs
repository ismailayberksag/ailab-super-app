using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Task;

public class CreateTaskDto
{
    [Required(ErrorMessage = "Task başlığı gereklidir")]
    [MaxLength(200, ErrorMessage = "Task başlığı maksimum 200 karakter olabilir")]
    public string Title { get; set; } = default!;

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Proje ID gereklidir")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "Atanacak kullanıcı ID gereklidir")]
    public Guid AssigneeId { get; set; } // Must be a project member

    public DateTime? DueDate { get; set; }
}

