using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Announcement
{
    public class CreateAnnouncementDto
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [MaxLength(200, ErrorMessage = "Başlık maksimum 200 karakter olabilir")]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "İçerik gereklidir")]
        [MaxLength(1000, ErrorMessage = "İçerik maksimum 1000 karakter olabilir")]
        public string Content { get; set; } = default!;

        [Required]
        public AnnouncementScope Scope { get; set; }

        // Project ve Individual scope'larında kullanılabilir
        public List<Guid>? TargetProjectIds { get; set; }

        // Individual scope'unda kullanılabilir
        public List<Guid>? TargetUserIds { get; set; }
    }
}
