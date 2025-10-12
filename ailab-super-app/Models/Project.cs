namespace ailab_super_app.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    // Navigations (mapping'te kullanıyoruz)
    public List<ProjectMember> Members { get; set; } = [];
    public List<TaskItem> Tasks { get; set; } = [];
    public List<Report> Reports { get; set; } = [];

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
