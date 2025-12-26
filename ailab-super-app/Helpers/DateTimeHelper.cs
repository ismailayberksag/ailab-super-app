namespace ailab_super_app.Helpers;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    public static DateTime GetTurkeyTime()
    {
        // Linux/Docker ortamlarında TimeZone ID farklı olabilir ("Europe/Istanbul")
        // Bu yüzden güvenli bir yaklaşım kullanacağız: UTC + 3 saat sabiti
        return DateTime.UtcNow.AddHours(3);
    }
}
