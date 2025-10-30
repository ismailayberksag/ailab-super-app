using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Report;

public class ReviewReportDto
{
    [Required(ErrorMessage = "Durum gereklidir (Approved/Rejected)")]
    [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Geçerli durumlar: Approved veya Rejected")] 
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Notlar maksimum 1000 karakter olabilir")]
    public string? ReviewNotes { get; set; }
}



