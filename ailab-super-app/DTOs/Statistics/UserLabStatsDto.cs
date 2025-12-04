using System;

namespace ailab_super_app.DTOs.Statistics
{
    public class UserLabStatsDto
    {
        public TimeSpan TotalTimeSpent { get; set; }  // Labda geçirilen toplam süre
        public DateTime? LastEntryDate { get; set; }  // Son giriş tarihi
    }
}
