namespace ailab_super_app.DTOs.Statistics
{
    public class LabStatusDto
    {
        public int CurrentOccupancyCount { get; set; } // İçerideki kişi sayısı
        public int TotalCapacity { get; set; }        // Girebilecek toplam kişi sayısı (aktif RFID kart sayısı)
        public int TeammatesInsideCount { get; set; } // İçerideki takım arkadaşı sayısı
        public int TotalTeammatesCount { get; set; }  // Toplam takım arkadaşı sayısı (kullanıcının projelerindeki tekil üyeler)
    }
}
