namespace ailab_super_app.Models;

public class AnnouncementProject
{
    public Guid AnnouncementId { get; set; }
    public Guid ProjectId { get; set; }

    // Navigation Properties
    public Announcement Announcement { get; set; } = default!;
    public Project Project { get; set; } = default!;
}