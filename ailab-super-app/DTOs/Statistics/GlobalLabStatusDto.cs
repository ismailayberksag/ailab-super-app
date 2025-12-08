namespace ailab_super_app.DTOs.Statistics
{
    public class GlobalLabStatusDto
    {
        public int CurrentOccupancyCount { get; set; }
        public int TotalCapacity { get; set; }
        public List<string> PeopleInside { get; set; } = new();
    }
}
