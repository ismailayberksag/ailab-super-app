namespace ailab_super_app.Models;

public class ScoreHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal PointsChanged { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public User User { get; set; } = default!;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}