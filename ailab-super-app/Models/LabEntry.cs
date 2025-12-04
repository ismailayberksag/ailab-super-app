using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

public class LabEntry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? CardUid { get; set; }

    public EntryType EntryType { get; set; }

    public DateTime EntryTime { get; set; } = DateTime.UtcNow;

    public DateTime? ExitTime { get; set; } // Çıkış zamanı

    public int? DurationMinutes { get; set; } // İçeride kalınan süre

    public Guid? RoomId { get; set; } // Hangi odada?

    public Guid? RfidCardId { get; set; } // Hangi kartla?

    public string? Notes { get; set; }

    // Navigation Property
    public User User { get; set; } = default!;
}