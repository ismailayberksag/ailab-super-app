namespace ailab_super_app.DTOs.Statistics
{
    public class LeaderboardUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
