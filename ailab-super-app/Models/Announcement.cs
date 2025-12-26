using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;

    public AnnouncementScope Scope { get; set; } = AnnouncementScope.Global;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public List<AnnouncementProject> TargetProjects { get; set; } = [];
    public List<AnnouncementUser> TargetUsers { get; set; } = [];

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}