namespace ailab_super_app.DTOs.Rfid;

public class DoorStatusResponseDto
{
    public Guid RoomId { get; set; }
    
    public string RoomName { get; set; } = string.Empty;
    
    public bool IsOpen { get; set; }
    
    public DateTime LastUpdatedAt { get; set; }
}
