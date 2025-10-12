namespace ailab_super_app.Models;

public class CardRegistrationPending
{
    public Guid Id { get; set; }

    public string CardUid { get; set; } = string.Empty;

    public Guid InitiatedBy { get; set; }

    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}