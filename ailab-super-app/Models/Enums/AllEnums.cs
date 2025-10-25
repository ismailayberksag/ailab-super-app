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
    Pending,
    Approved,
    Rejected,
    Revision
}

public enum EntryType
{
    Entry,
    Exit
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