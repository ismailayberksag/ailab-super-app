namespace ailab_super_app.Configuration
{
    public class JwtSettings
    {
        public string Secret { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int AccessTokenExpirationMinutes { get; set; } = 60; // Default 60 dakika
        public int RefreshTokenExpirationDays { get; set; } = 7; // Default 7 gün
    }
}
