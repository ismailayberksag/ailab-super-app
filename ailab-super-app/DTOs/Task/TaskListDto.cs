using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.DTOs.Task;

public class TaskListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public TaskStatus Status { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ProjectName { get; set; }
}

