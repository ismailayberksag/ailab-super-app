using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

public class LabEntry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? CardUid { get; set; }

    public EntryType EntryType { get; set; }

    public DateTime EntryTime { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public User User { get; set; } = default!;
}