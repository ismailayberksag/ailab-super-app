using ailab_super_app.Models.Enums;

namespace ailab_super_app.DTOs.Room
{
    public class RoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public RoomAccessMode AccessMode { get; set; }
    }

    public class UpdateRoomAccessModeDto
    {
        public RoomAccessMode Mode { get; set; }
    }
}
