using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Report;

public class CreateReportRequestDto
{
    // If ProjectId is null => individual request, TargetUserIds must be provided
    public Guid? ProjectId { get; set; }

    [Required(ErrorMessage = "Rapor başlığı gereklidir")]
    [MaxLength(200, ErrorMessage = "Rapor başlığı maksimum 200 karakter olabilir")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Açıklama maksimum 1000 karakter olabilir")]
    public string? Description { get; set; }

    // Optional; if provided, must be in future (validated in service/controller)
    public DateTime? DueDate { get; set; }

    [MaxLength(50, ErrorMessage = "Period tipi maksimum 50 karakter olabilir")]
    public PeriodType? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    // Required when ProjectId is null; when ProjectId present, optional subset of project members
    public List<Guid>? TargetUserIds { get; set; }
}



