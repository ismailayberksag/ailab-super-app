using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.Models;

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}

public class User : IdentityUser<Guid>  
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public int TotalScore { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RfidCard> RfidCards { get; set; } = new List<RfidCard>();
    public virtual ICollection<LabEntry> LabEntries { get; set; } = new List<LabEntry>();
    public virtual ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Report> SubmittedReports { get; set; } = new List<Report>();
    public virtual ICollection<ScoreHistory> ScoreHistory { get; set; } = new List<ScoreHistory>();
    public virtual ICollection<AnnouncementUser> AnnouncementUsers { get; set; } = new List<AnnouncementUser>();

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}