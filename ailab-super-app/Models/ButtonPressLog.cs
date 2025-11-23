namespace ailab_super_app.Models;

public class ButtonPressLog
{
    public Guid Id { get; set; }

    public Guid ButtonId { get; set; }  // PhysicalButton Id

    public Guid RoomId { get; set; }  // Hangi odanın kapısı açıldı

    public string ButtonUid { get; set; } = default!;  // Butonun fiziksel UUID/MAC adresi

    public DateTime PressedAt { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; }  // İşlem başarılı mı?

    public string? ErrorMessage { get; set; }  // Hata varsa mesajı

    // Navigation Properties
    public PhysicalButton Button { get; set; } = default!;
    public Room Room { get; set; } = default!;
}


