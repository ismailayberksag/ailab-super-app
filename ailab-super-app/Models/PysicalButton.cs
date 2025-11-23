namespace ailab_super_app.Models;

public class PhysicalButton
{
    public Guid Id { get; set; }

    public string ButtonUid { get; set; } = default!;  // Butonun fiziksel UUID/MAC adresi

    public Guid RoomId { get; set; }  // Hangi odada bulunduğu

    public string? AssignedAction { get; set; }  // "OpenDoor", "RegisterCard", vb.

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Access token olmadan, sadece izin verilen butonlar çalışır
    public bool RequiresAuthentication { get; set; } = true;

    // Navigation Property
    public Room Room { get; set; } = default!;
}