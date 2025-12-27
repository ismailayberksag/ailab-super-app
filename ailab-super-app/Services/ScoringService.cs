using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services;

public class ScoringService : IScoringService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ScoringService> _logger;

    public ScoringService(AppDbContext context, ILogger<ScoringService> _logger)
    {
        _context = context;
        this._logger = _logger;
    }

    public async Task AddScoreAsync(Guid userId, decimal points, string reason, string? referenceType = null, Guid? referenceId = null)
    {
        if (points == 0) return;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Puan eklenmek istenen kullanıcı bulunamadı: {UserId}", userId);
            return;
        }

        // 1. Kullanıcının total puanını güncelle
        user.TotalScore += points;
        user.UpdatedAt = DateTimeHelper.GetTurkeyTime();

        // 2. ScoreHistory kaydı oluştur
        var scoreHistory = new ScoreHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PointsChanged = points,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = DateTimeHelper.GetTurkeyTime(),
            IsDeleted = false
        };

        _context.ScoreHistory.Add(scoreHistory);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Kullanıcıya {Points} puan eklendi. Sebep: {Reason}, User: {UserId}", points, reason, userId);
    }

    public decimal GetPointsByCategory(int category)
    {
        return category switch
        {
            0 => 0m,
            1 => 0.25m,
            2 => 1.00m,
            3 => 1.50m,
            _ => 0m
        };
    }
}
