namespace ailab_super_app.DTOs.Rfid;

public class ButtonPressResponseDto
{
    public bool Success { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public bool DoorShouldOpen { get; set; }
}

