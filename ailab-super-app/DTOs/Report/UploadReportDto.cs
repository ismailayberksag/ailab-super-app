using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Report;

public class UploadReportDto
{
    [Required(ErrorMessage = "Rapor talebi ID gereklidir")]
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "Rapor başlığı gereklidir")]
    [MaxLength(200, ErrorMessage = "Rapor başlığı maksimum 200 karakter olabilir")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } // Added Description

    [Required(ErrorMessage = "PDF dosyası gereklidir")]
    public IFormFile PdfFile { get; set; } = default!;

    public PeriodType? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}