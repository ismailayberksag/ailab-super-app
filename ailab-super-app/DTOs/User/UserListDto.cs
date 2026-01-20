using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.User;

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public UserStatus Status { get; set; }
    public decimal TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProfileImageUrl { get; set; }
    public List<string> Roles { get; set; } = new();
}