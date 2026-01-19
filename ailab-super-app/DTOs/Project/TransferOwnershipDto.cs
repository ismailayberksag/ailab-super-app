using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Project;

public class TransferOwnershipDto
{
    [Required(ErrorMessage = "Mevcut captain ID'si zorunludur")]
    public Guid CurrentCaptainId { get; set; }

    [Required(ErrorMessage = "Yeni captain ID'si zorunludur")]
    public Guid NewCaptainId { get; set; }
}
