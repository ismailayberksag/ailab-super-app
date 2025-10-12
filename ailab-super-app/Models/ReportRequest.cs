namespace ailab_super_app.Models;

public class ReportRequest
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? RequestedBy { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    public string? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }

    public DateTime? PeriodEnd { get; set; }

    // Navigation Property
    public Project Project { get; set; } = default!;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}