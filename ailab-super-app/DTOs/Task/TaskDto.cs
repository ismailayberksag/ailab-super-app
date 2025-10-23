using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.DTOs.Task;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}

