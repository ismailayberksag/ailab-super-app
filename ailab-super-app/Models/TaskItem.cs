namespace ailab_super_app.Models;
using ailab_super_app.Models.Enums;

public class TaskItem
{
    public Guid Id { get; set; }  

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Kime atandı
    public Guid AssigneeId { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("AssigneeId")]
    public User? User { get; set; } // Navigasyon özelliği eklendi

    // Kim oluşturdu
    public Guid CreatedBy { get; set; }

    // Proje ilişkisi
    public Guid? ProjectId { get; set; } 
    public Project? Project { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}