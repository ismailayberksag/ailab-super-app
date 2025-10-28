using System;

namespace ailab_super_app.DTOs.Announcement
{
    public class TargetProjectInfo
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = default!;
        public int MemberCount { get; set; }
        public int ReadCount { get; set; }
    }
}