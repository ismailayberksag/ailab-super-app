namespace ailab_super_app.Models;

public class RfidCard
{
    public Guid Id { get; set; }

    public string CardUid { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? RegisteredBy { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? LastUsed { get; set; }

    public string? Notes { get; set; }

    // Navigation Property
    public User? User { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}