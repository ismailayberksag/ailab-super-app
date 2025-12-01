using ailab_super_app.Models;

namespace ailab_super_app.DTOs.User;

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserStatus Status { get; set; }
    public int TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}