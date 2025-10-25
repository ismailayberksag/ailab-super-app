namespace ailab_super_app.Models;
using ailab_super_app.Models.Enums;

public class RfidReader
{
    public Guid Id { get; set; }
    
    public string ReaderUid { get; set; } = string.Empty; // Reader'ın benzersiz ID'si
    
    public string Name { get; set; } = string.Empty; // "Dış Giriş Reader", "İç Çıkış Reader"
    
    public string? Description { get; set; }
    
    public Guid RoomId { get; set; } // Hangi odada bulunduğu
    
    public ReaderLocation Location { get; set; } // Inside, Outside
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public Room Room { get; set; } = default!;
}