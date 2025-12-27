namespace ailab_super_app.Services.Interfaces;

public interface IScoringService
{
    /// <summary>
    /// Kullanıcıya puan ekler veya çıkarır, geçmiş kaydı oluşturur.
    /// </summary>
    Task AddScoreAsync(Guid userId, decimal points, string reason, string? referenceType = null, Guid? referenceId = null);
    
    /// <summary>
    /// Belirli bir kategorinin puan karşılığını döner.
    /// </summary>
    decimal GetPointsByCategory(int category);
}
