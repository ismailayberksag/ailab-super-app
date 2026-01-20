using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User;

public class UpdateUserDto
{
    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [MaxLength(20)]
    public string Phone { get; set; } = default!;
}

public class UpdateEmailDto
{
    [Required(ErrorMessage = "E-posta adresi zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string NewEmail { get; set; } = default!;
}