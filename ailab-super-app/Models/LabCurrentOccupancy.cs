namespace ailab_super_app.Models;

public class LabCurrentOccupancy
{
    public Guid UserId { get; set; }

    public DateTime EntryTime { get; set; }

    public string? CardUid { get; set; }

    public Guid? RoomId { get; set; }

    // Navigation Property
    public User User { get; set; } = default!;
}