using System;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Announcement
{
    public class AnnouncementListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public AnnouncementScope Scope { get; set; }
        public DateTime CreatedAt { get; set; }

        //Liste ekranı için kullanıcıya göre okunma durumu
        public bool IsRead { get; set; } = false;

        // kısa önizleme opsiyonel
        public string? Preview { get; set; }
    }
}
