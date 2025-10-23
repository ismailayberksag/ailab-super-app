using System.ComponentModel.DataAnnotations;
using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.DTOs.Task;

public class UpdateTaskStatusDto
{
    [Required(ErrorMessage = "Status gereklidir")]
    public TaskStatus Status { get; set; }
}

