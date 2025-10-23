namespace ailab_super_app.DTOs.Project;

public class ProjectListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public List<string> CaptainNames { get; set; } = new();
}

