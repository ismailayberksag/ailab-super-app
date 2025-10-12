using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

public class RoomAccess
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    // Okutulan kart ve kart sahibi
    public Guid RfidCardId { get; set; }
    public RfidCard RfidCard { get; set; } = default!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Dış kapı / iç taraftaki okuyucu yönü: Entry = içeri giriş; Exit = çıkış
    public EntryType Direction { get; set; }

    // Yetkilendirme sonucu
    public bool IsAuthorized { get; set; }
    public string? DenyReason { get; set; }

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    // Ham log (okuyucudan gelen raw data vs.)
    public string? RawPayload { get; set; }

    // Bu access kaydından üretilen oturum bağlantıları (opsiyonel)
    public Guid? CreatedEntryId { get; set; }
    public LabEntry? CreatedEntry { get; set; }
}