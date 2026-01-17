using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Report;

public class ReviewReportDto
{
    [Required(ErrorMessage = "Durum gereklidir")]
    public ReportStatus Status { get; set; } // Approved or Rejected

    [MaxLength(1000, ErrorMessage = "Notlar maksimum 1000 karakter olabilir")]
    public string? Reason { get; set; } // ReviewNotes or Reason. Let's use Reason as I used in Service logic.
}