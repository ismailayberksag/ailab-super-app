namespace ailab_super_app.Models;

public class AnnouncementUser
{
    public Guid AnnouncementId { get; set; }
    public Guid UserId { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    public Announcement Announcement { get; set; } = default!;
    public User User { get; set; } = default!;
}