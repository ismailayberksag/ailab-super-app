namespace ailab_super_app.Models.Enums;

public enum TaskStatus
{
    Todo,
    InProgress,
    Done,
    Cancelled
}

public enum AnnouncementScope
{
    Global,      // Herkese
    Project,     // Belirli projelere
    Individual   // Belirli kullanıcılara
}

public enum ReportStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Expired
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}

public enum ReaderLocation
{
    Inside,   
    Outside   
}

public enum ReportRequestStatus
{
    Pending,        // Talep açıldı, henüz yükleme yok
    Submitted,      // En az bir PDF yüklendi
    UnderReview,    // Talep sahibi görüntüledi/inceleme aşamasında
    Approved,       // Onaylandı
    Rejected       // Reddedildi
}

public enum PeriodType
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annual,
    Custom
}

public enum BugType
{
    VisualError = 1,        // UI / Görsel Hata
    FunctionalError = 2,    // İş Akışı / Fonksiyonel Hata
    PerformanceIssue = 3,   // Performans Problemi
    CrashOrFreeze = 4,      // Çökme / Donma
    AuthorizationIssue = 5, // Yetkilendirme / Erişim Problemi
    Other = 99              // Diğer
}

public enum PlatformType
{
    Web = 1,
    Mobile = 2
}

public enum AuthProvider
{
    Legacy = 0,      // Mevcut ASP.NET Identity sistemi
    Firebase = 1     // Firebase Authentication
}