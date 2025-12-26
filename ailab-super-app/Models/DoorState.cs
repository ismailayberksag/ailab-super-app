namespace ailab_super_app.Models;

public class DoorState
{
    public Guid Id { get; set; }
    
    public Guid RoomId { get; set; }
    
    public bool IsOpen { get; set; } = false;
    
    public DateTime LastUpdatedAt { get; set; }
    
    // Navigation Property
    public Room Room { get; set; } = default!;
}
