namespace ailab_super_app.DTOs.Project;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Nullable Creator info
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    
    // Task statistics with breakdown
    public int TaskCount { get; set; }
    public int TodoTaskCount { get; set; }
    public int InProgressTaskCount { get; set; }
    public int DoneTaskCount { get; set; }
    public int CancelledTaskCount { get; set; }
    
    // Member info
    public int MemberCount { get; set; }
    public List<ProjectMemberDto> Captains { get; set; } = new();
    public List<ProjectMemberDto> Members { get; set; } = new();
}

