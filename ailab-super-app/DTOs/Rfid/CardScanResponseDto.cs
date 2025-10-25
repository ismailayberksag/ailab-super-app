namespace ailab_super_app.DTOs.Rfid;

public class CardScanResponseDto
{
    public bool Success { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public bool DoorShouldOpen { get; set; }
    
    public string? UserName { get; set; }
    
    public bool IsEntry { get; set; }
}
