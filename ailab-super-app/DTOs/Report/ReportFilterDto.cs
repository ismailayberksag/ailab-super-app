namespace ailab_super_app.DTOs.Report;

public class ReportFilterDto
{
    public Guid? ProjectId { get; set; }
    public Guid? RequestId { get; set; }
    public string? Status { get; set; }

    public DateTime? SubmittedFrom { get; set; }
    public DateTime? SubmittedTo { get; set; }
}



