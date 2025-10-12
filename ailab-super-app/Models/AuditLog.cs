namespace ailab_super_app.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? TableName { get; set; }

    public Guid? RecordId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}