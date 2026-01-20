using ailab_super_app.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User;

public class UpdateUserStatusDto
{
    [Required]
    public UserStatus Status { get; set; }
}