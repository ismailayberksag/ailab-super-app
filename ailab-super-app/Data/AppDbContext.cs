using ailab_super_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Data
{
    public class AppDbContext : IdentityDbContext<User, AppRole, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Mevcut DbSet'ler
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomAccess> RoomAccesses { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<RfidCard> RfidCards { get; set; }
        public DbSet<PhysicalButton> PhysicalButtons { get; set; }
        public DbSet<CardRegistrationPending> CardRegistrationPendings { get; set; }
        public DbSet<LabEntry> LabEntries { get; set; }
        public DbSet<LabCurrentOccupancy> LabCurrentOccupancy { get; set; }
        public DbSet<ReportRequest> ReportRequests { get; set; }
        public DbSet<ScoreHistory> ScoreHistory { get; set; }
        public DbSet<AnnouncementProject> AnnouncementProjects { get; set; }
        public DbSet<AnnouncementUser> AnnouncementUsers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.HasDefaultSchema("app");

            // Identity tablolarını yeniden adlandır
            b.Entity<User>().ToTable("users");
            b.Entity<AppRole>().ToTable("roles");
            b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
            b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
            b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
            b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
            b.Entity<UserRole>().ToTable("user_roles"); // Custom UserRole kullan

            // User konfigürasyonu
            b.Entity<User>(e =>
            {
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.FullName).HasMaxLength(200);
                e.Property(x => x.Phone).HasMaxLength(20);
                e.Property(x => x.AvatarUrl).HasMaxLength(500);

                // Yeni: SchoolNumber konfigurasyonu ve benzersiz index
                e.Property(x => x.SchoolNumber).HasMaxLength(50);
                e.HasIndex(x => x.SchoolNumber).IsUnique();
            });

            // AppRole konfigürasyonu
            b.Entity<AppRole>(e =>
            {
                e.Property(x => x.Description).HasMaxLength(500);
                e.Property(x => x.Permissions).HasColumnType("jsonb");
            });

            // UserRole konfigürasyonu
            b.Entity<UserRole>(e =>
            {
                e.HasOne(ur => ur.User)
                 .WithMany(u => u.UserRoles)
                 .HasForeignKey(ur => ur.UserId);

                e.HasOne(ur => ur.Role)
                 .WithMany(r => r.UserRoles)
                 .HasForeignKey(ur => ur.RoleId);
            });

            // RFID Cards
            b.Entity<RfidCard>(e =>
            {
                e.ToTable("rfid_cards");
                e.HasKey(x => x.Id);
                e.Property(x => x.CardUid).IsRequired().HasMaxLength(50);
                e.Property(x => x.Notes).HasMaxLength(500);
                e.HasIndex(x => x.CardUid).IsUnique();

                e.HasOne(x => x.User)
                 .WithMany(u => u.RfidCards)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.RegisteredBy)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Physical Buttons
            b.Entity<PhysicalButton>(e =>
            {
                e.ToTable("physical_buttons");
                e.HasKey(x => x.Id);
                e.Property(x => x.ButtonUid).IsRequired().HasMaxLength(100);
                e.Property(x => x.AssignedAction).HasMaxLength(50);
                e.HasIndex(x => x.ButtonUid).IsUnique();
            });

            // Card Registration Pending
            b.Entity<CardRegistrationPending>(e =>
            {
                e.ToTable("card_registration_pending");
                e.HasKey(x => x.Id);
                e.Property(x => x.CardUid).IsRequired().HasMaxLength(50);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.InitiatedBy)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Lab Entries
            b.Entity<LabEntry>(e =>
            {
                e.ToTable("lab_entries");
                e.HasKey(x => x.Id);
                e.Property(x => x.EntryType).HasConversion<string>();
                e.Property(x => x.CardUid).HasMaxLength(50);

                e.HasIndex(x => new { x.UserId, x.EntryTime });
                e.HasIndex(x => x.EntryTime);

                e.HasOne(x => x.User)
                 .WithMany(u => u.LabEntries)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Lab Current Occupancy
            b.Entity<LabCurrentOccupancy>(e =>
            {
                e.ToTable("lab_current_occupancy");
                e.HasKey(x => x.UserId);
                e.Property(x => x.CardUid).HasMaxLength(50);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Report Requests
            b.Entity<ReportRequest>(e =>
            {
                e.ToTable("report_requests");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.PeriodType).HasMaxLength(20);

                e.HasOne(x => x.Project)
                 .WithMany()
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.RequestedBy)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Score History
            b.Entity<ScoreHistory>(e =>
            {
                e.ToTable("score_history");
                e.HasKey(x => x.Id);
                e.Property(x => x.Reason).IsRequired().HasMaxLength(500);
                e.Property(x => x.ReferenceType).HasMaxLength(50);

                e.HasIndex(x => new { x.UserId, x.CreatedAt });

                e.HasOne(x => x.User)
                 .WithMany(u => u.ScoreHistory)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.CreatedBy)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Audit Logs
            b.Entity<AuditLog>(e =>
            {
                e.ToTable("audit_logs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Action).IsRequired().HasMaxLength(100);
                e.Property(x => x.TableName).HasMaxLength(50);
                e.Property(x => x.OldValues).HasColumnType("jsonb");
                e.Property(x => x.NewValues).HasColumnType("jsonb");

                e.HasIndex(x => new { x.UserId, x.CreatedAt });
                e.HasIndex(x => new { x.TableName, x.RecordId });

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // System Settings (mevcut konfigürasyonu güncelle)
            b.Entity<SystemSetting>(e =>
            {
                e.ToTable("system_settings");
                e.HasKey(x => x.Key);
                e.Property(x => x.Key).HasMaxLength(100);
                e.Property(x => x.DataType).HasMaxLength(20);
                e.Property(x => x.Category).HasMaxLength(50);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.UpdatedBy)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Project (mevcut konfigürasyonu koru)
            b.Entity<Project>(e =>
            {
                e.ToTable("projects");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.UpdatedAt).IsRequired(false);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.CreatedBy)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Project Member (mevcut konfigürasyonu güncelle)
            b.Entity<ProjectMember>(e =>
            {
                e.ToTable("project_members");
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
                e.Property(x => x.Role).IsRequired().HasMaxLength(100);
                e.Property(x => x.AddedAt).IsRequired();

                e.HasOne(x => x.Project)
                 .WithMany(p => p.Members)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User)
                 .WithMany(u => u.ProjectMemberships)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Tasks (mevcut konfigürasyonu güncelle)
            b.Entity<TaskItem>(e =>
            {
                e.ToTable("tasks");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.UpdatedAt).IsRequired(false);

                e.HasIndex(x => new { x.AssigneeId, x.Status });
                e.HasIndex(x => new { x.ProjectId, x.Status });
                e.HasIndex(x => x.DueDate);

                e.HasOne<User>()
                 .WithMany(u => u.AssignedTasks)
                 .HasForeignKey(x => x.AssigneeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<User>()
                 .WithMany(u => u.CreatedTasks)
                 .HasForeignKey(x => x.CreatedBy)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Project)
                 .WithMany(p => p.Tasks)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Reports (mevcut konfigürasyonu güncelle)
            b.Entity<Report>(e =>
            {
                e.ToTable("reports");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
                e.Property(x => x.PeriodType).HasMaxLength(20);
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.SubmittedAt).IsRequired();

                e.HasIndex(x => new { x.ProjectId, x.SubmittedAt });
                e.HasIndex(x => new { x.Status, x.SubmittedAt });

                e.HasOne(x => x.Project)
                 .WithMany(p => p.Reports)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<User>()
                 .WithMany(u => u.SubmittedReports)
                 .HasForeignKey(x => x.SubmittedBy)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.ReviewedBy)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.Request)
                 .WithMany()
                 .HasForeignKey(x => x.RequestId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Rooms (mevcut konfigürasyonu koru)
            b.Entity<Room>(e =>
            {
                e.ToTable("rooms");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.UpdatedAt).IsRequired(false);
            });

            b.Entity<RoomAccess>(e =>
            {
                e.ToTable("room_accesses");
                e.HasKey(x => x.Id);
                e.Property(x => x.Direction).HasConversion<string>();
                e.Property(x => x.DenyReason).HasMaxLength(200);

                e.HasIndex(x => new { x.RoomId, x.AccessedAt });
                e.HasIndex(x => new { x.UserId, x.AccessedAt });

                e.HasOne(x => x.Room)
                 .WithMany()
                 .HasForeignKey(x => x.RoomId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.RfidCard)
                 .WithMany()
                 .HasForeignKey(x => x.RfidCardId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.CreatedEntry)
                 .WithMany()
                 .HasForeignKey(x => x.CreatedEntryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Announcements (mevcut konfigürasyonu koru)
            b.Entity<Announcement>(e =>
            {
                e.ToTable("announcements");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.Content).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.Scope).HasConversion<string>();

                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.CreatedBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Announcement Projects (mevcut konfigürasyonu koru)
            b.Entity<AnnouncementProject>(e =>
            {
                e.ToTable("announcement_projects");
                e.HasKey(x => new { x.AnnouncementId, x.ProjectId });

                e.HasOne(x => x.Announcement)
                 .WithMany(a => a.TargetProjects)
                 .HasForeignKey(x => x.AnnouncementId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Project)
                 .WithMany()
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Announcement Users (mevcut konfigürasyonu güncelle)
            b.Entity<AnnouncementUser>(e =>
            {
                e.ToTable("announcement_users");
                e.HasKey(x => new { x.AnnouncementId, x.UserId });
                e.Property(x => x.IsRead).HasDefaultValue(false);

                e.HasOne(x => x.Announcement)
                 .WithMany(a => a.TargetUsers)
                 .HasForeignKey(x => x.AnnouncementId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User)
                 .WithMany(u => u.AnnouncementUsers)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Refresh Token Konfigürasyonu
            b.Entity<RefreshToken>(e =>
            {
                e.ToTable("refresh_tokens");
                e.HasKey(x => x.Id);

                e.Property(x => x.Token).IsRequired().HasMaxLength(500);
                e.Property(x => x.CreatedByIp).IsRequired().HasMaxLength(50);
                e.Property(x => x.RevokedByIp).HasMaxLength(50);
                e.Property(x => x.ReplacedByToken).HasMaxLength(500);

                // Index'ler - performans için
                e.HasIndex(x => x.Token).IsUnique();
                e.HasIndex(x => new { x.UserId, x.ExpiresAt });
                e.HasIndex(x => x.ExpiresAt);

                // Foreign key - User ile ilişki
                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Soft delete için global filter
            b.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<Project>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<TaskItem>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<Report>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<Announcement>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<RfidCard>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<Room>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<ScoreHistory>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<ReportRequest>().HasQueryFilter(x => !x.IsDeleted);
            b.Entity<ProjectMember>().HasQueryFilter(x => !x.IsDeleted);
        }
    }
}