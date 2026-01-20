using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

using ailab_super_app.Models.Enums; // AuthProvider için eklendi

namespace ailab_super_app.Models;

public class User : IdentityUser<Guid>  
{
    // Firebase Authentication Integration
    [MaxLength(128)]
    public string? FirebaseUid { get; set; }
    
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Legacy;
    
    public DateTime? MigratedToFirebaseAt { get; set; }

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    // Yeni: Okul numarası
    [MaxLength(50)]
    public string? SchoolNumber { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public decimal TotalScore { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

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