using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.BugReport;

public class CreateBugReportDto
{
    [Required(ErrorMessage = "Platform bilgisi gereklidir.")]
    public PlatformType Platform { get; set; }

    [Required(ErrorMessage = "Sayfa/Ekran bilgisi gereklidir.")]
    [MaxLength(250, ErrorMessage = "Sayfa bilgisi 250 karakterden uzun olamaz.")]
    public string PageInfo { get; set; } = default!;

    [Required(ErrorMessage = "Hata tipi gereklidir.")]
    public BugType BugType { get; set; }

    [Required(ErrorMessage = "Hata açıklaması zorunludur.")]
    [MaxLength(2000, ErrorMessage = "Hata açıklaması 2000 karakterden uzun olamaz.")]
    public string Description { get; set; } = default!;
}
