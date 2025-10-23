using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Task;

public class UpdateTaskDto
{
    [MaxLength(200, ErrorMessage = "Task başlığı maksimum 200 karakter olabilir")]
    public string? Title { get; set; }

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }

    public TaskStatus? Status { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTime? DueDate { get; set; }
}

