using System;

namespace ailab_super_app.DTOs.Announcement
{
    public class AnnouncementRecipientDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}