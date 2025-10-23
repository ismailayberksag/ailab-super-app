namespace ailab_super_app.DTOs.Project;

public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = default!;
    public DateTime AddedAt { get; set; }
}

