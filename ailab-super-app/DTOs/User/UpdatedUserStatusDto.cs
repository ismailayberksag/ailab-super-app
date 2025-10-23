using System.ComponentModel.DataAnnotations;
using ailab_super_app.Models;

namespace ailab_super_app.DTOs.User;

public class UpdateUserStatusDto
{
    [Required(ErrorMessage = "Kullanıcı durumu gereklidir")]
    public UserStatus Status { get; set; }
}