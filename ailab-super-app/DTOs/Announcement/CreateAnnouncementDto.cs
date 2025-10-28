using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Announcement;

public class CreateAnnouncementDto : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required]
    public string Content { get; set; } = default!;

    [Required]
    public AnnouncementScope Scope { get; set; }

    // Project ve Individual scope'larında kullanılabilir
    public List<Guid>? TargetProjectIds { get; set; }

    // Individual scope'unda kullanılabilir
    public List<Guid>? TargetUserIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Scope == AnnouncementScope.Project &&
            (TargetProjectIds == null || !TargetProjectIds.Any()))
        {
            yield return new ValidationResult(
                "Proje duyurusu için en az bir proje seçilmelidir",
                new[] { nameof(TargetProjectIds) }
            );
        }

        if (Scope == AnnouncementScope.Individual &&
            (TargetUserIds == null || !TargetUserIds.Any()))
        {
            yield return new ValidationResult(
                "Bireysel duyuru için en az bir kullanıcı seçilmelidir",
                new[] { nameof(TargetUserIds) }
            );
        }

        if (Scope == AnnouncementScope.Global &&
            ((TargetProjectIds?.Any() ?? false) || (TargetUserIds?.Any() ?? false)))
        {
            yield return new ValidationResult(
                "Global duyuruda hedef proje veya kullanıcı belirtilemez",
                new[] { nameof(Scope) }
            );
        }
    }
}