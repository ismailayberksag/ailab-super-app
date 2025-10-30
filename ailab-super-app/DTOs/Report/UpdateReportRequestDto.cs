using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Report;

public class UpdateReportRequestDto
{
    [MaxLength(200, ErrorMessage = "Rapor başlığı maksimum 200 karakter olabilir")]
    public string? Title { get; set; }

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(50, ErrorMessage = "Period tipi maksimum 50 karakter olabilir")]
    public PeriodType? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    // Optional update of targets
    public List<Guid>? TargetUserIds { get; set; }
}



