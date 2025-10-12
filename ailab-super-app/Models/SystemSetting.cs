namespace ailab_super_app.Models;

public class SystemSetting
{
    // PK: AppDbContext'te HasKey(x => x.Key) dedik
    public string Key { get; set; } = default!;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? DataType { get; set; }   // int, string, bool gibi tip bilgisi
    public string? Category { get; set; }   // hangi kategoriye ait (ör: "Security", "UI")
    public Guid? UpdatedBy { get; set; }    // kim güncelledi
    public DateTime? UpdatedAt { get; set; } // ne zaman güncellendi
}
