namespace ailab_super_app.DTOs.Role;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
    public int UserCount { get; set; }
}

