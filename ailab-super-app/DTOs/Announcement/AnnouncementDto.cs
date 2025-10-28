using System;
using System.Collections.Generic;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Announcement
{
    public class AnnouncementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public AnnouncementScope Scope { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = default!;
        public string? CreatedByEmail { get; set; }

        //Scope = Project ise doldurulacak
        public List<TargetProjectInfo>? AnnouncementProjects { get; set; }

        // Scope = Individual ise doldurulacak
        public List<AnnouncementRecipientDto>? AnnouncementUsers { get; set; }
    }

    public class TargetProjectInfo
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = default!;
        public int MemberCount { get; set; }
        public int ReadCount { get; set; }
    }
}
