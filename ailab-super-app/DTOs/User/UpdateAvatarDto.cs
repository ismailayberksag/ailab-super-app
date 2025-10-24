using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User;

public class UpdateAvatarDto
{
    [Required(ErrorMessage = "Avatar dosya adı gereklidir")]
    [RegularExpression(@"^(default|Man0[1-7]|Woman0[1-7])\.png$", 
        ErrorMessage = "Geçersiz avatar. Geçerli değerler: default.png, Man01.png-Man07.png, Woman01.png-Woman07.png")]
    public string AvatarFileName { get; set; } = default!;
}

