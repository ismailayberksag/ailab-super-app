using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReportSystem_ProjectBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_report_requests_projects_ProjectId",
                schema: "app",
                table: "report_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_report_requests_users_RequestedBy",
                schema: "app",
                table: "report_requests");

            // Güvenli silme: Tablo varsa sil, yoksa hata verme
            migrationBuilder.Sql("DROP TABLE IF EXISTS app.room_accesses;");

            migrationBuilder.DropIndex(
                name: "IX_report_requests_RequestedBy",
                schema: "app",
                table: "report_requests");

            migrationBuilder.DropColumn(
                name: "RequestedBy",
                schema: "app",
                table: "report_requests");

            // Güvenli kolon silme (IF EXISTS)
            migrationBuilder.Sql("ALTER TABLE app.lab_entries DROP COLUMN IF EXISTS \"CardUid\";");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries DROP COLUMN IF EXISTS \"EntryType\";");

            // Akıllı Rename: Sadece AvatarUrl varsa değiştir
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                  IF EXISTS(SELECT *
                    FROM information_schema.columns
                    WHERE table_schema = 'app' AND table_name = 'users' AND column_name = 'AvatarUrl')
                  THEN
                      ALTER TABLE app.users RENAME COLUMN ""AvatarUrl"" TO ""ProfileImageUrl"";
                  END IF;
                END $$;
            ");

            // Akıllı Rename: ProjectId -> CreatedBy
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                  IF EXISTS(SELECT *
                    FROM information_schema.columns
                    WHERE table_schema = 'app' AND table_name = 'report_requests' AND column_name = 'ProjectId')
                  THEN
                      ALTER TABLE app.report_requests RENAME COLUMN ""ProjectId"" TO ""CreatedBy"";
                  END IF;
                END $$;
            ");

            migrationBuilder.RenameIndex(
                name: "IX_report_requests_ProjectId",
                schema: "app",
                table: "report_requests",
                newName: "IX_report_requests_CreatedBy");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalScore",
                schema: "app",
                table: "users",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            // Güvenli AddColumn: IF NOT EXISTS
            migrationBuilder.Sql("ALTER TABLE app.tasks ADD COLUMN IF NOT EXISTS \"ScoreAssignedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE app.tasks ADD COLUMN IF NOT EXISTS \"ScoreCategory\" integer;");
            migrationBuilder.Sql("ALTER TABLE app.tasks ADD COLUMN IF NOT EXISTS \"ScoreProcessed\" boolean NOT NULL DEFAULT false;");

            migrationBuilder.AlterColumn<decimal>(
                name: "PointsChanged",
                schema: "app",
                table: "score_history",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "RequestId",
                schema: "app",
                table: "reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Güvenli AddColumn: reports ve report_requests
            migrationBuilder.Sql("ALTER TABLE app.reports ADD COLUMN IF NOT EXISTS \"Description\" text;");
            migrationBuilder.Sql("ALTER TABLE app.reports ADD COLUMN IF NOT EXISTS \"IsActive\" boolean NOT NULL DEFAULT false;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                schema: "app",
                table: "report_requests",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql("ALTER TABLE app.report_requests ADD COLUMN IF NOT EXISTS \"Status\" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE app.physical_buttons ADD COLUMN IF NOT EXISTS \"RoomId\" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';");

            // Güvenli AddColumn: lab_entries
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"DurationMinutes\" integer;");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"ExitTime\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"Notes\" text;");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"ReaderUid\" text;");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"RfidCardId\" uuid;");
            migrationBuilder.Sql("ALTER TABLE app.lab_entries ADD COLUMN IF NOT EXISTS \"RoomId\" uuid;");

            // Güvenli AddColumn: lab_current_occupancy
            migrationBuilder.Sql("ALTER TABLE app.lab_current_occupancy ADD COLUMN IF NOT EXISTS \"RoomId\" uuid;");

            migrationBuilder.CreateTable(
                name: "avatars",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bug_reports",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    PageInfo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BugType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bug_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bug_reports_users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "button_press_logs",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ButtonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ButtonUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PressedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_button_press_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_button_press_logs_physical_buttons_ButtonId",
                        column: x => x.ButtonId,
                        principalSchema: "app",
                        principalTable: "physical_buttons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_button_press_logs_rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "monthly_score_snapshots",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_score_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_audit_logs",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_audit_logs_reports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "app",
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_audit_logs_users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_request_projects",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PenaltyApplied = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_request_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_request_projects_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "app",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_request_projects_report_requests_ReportRequestId",
                        column: x => x.ReportRequestId,
                        principalSchema: "app",
                        principalTable: "report_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_avatars",
                schema: "app",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvatarId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_avatars", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_avatars_avatars_AvatarId",
                        column: x => x.AvatarId,
                        principalSchema: "app",
                        principalTable: "avatars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_avatars_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_physical_buttons_RoomId",
                schema: "app",
                table: "physical_buttons",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_bug_reports_ReportedByUserId",
                schema: "app",
                table: "bug_reports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_ButtonId",
                schema: "app",
                table: "button_press_logs",
                column: "ButtonId");

            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_PressedAt",
                schema: "app",
                table: "button_press_logs",
                column: "PressedAt");

            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_RoomId",
                schema: "app",
                table: "button_press_logs",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_report_audit_logs_PerformedByUserId",
                schema: "app",
                table: "report_audit_logs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_report_audit_logs_ReportId",
                schema: "app",
                table: "report_audit_logs",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_report_request_projects_ProjectId",
                schema: "app",
                table: "report_request_projects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_report_request_projects_ReportRequestId_ProjectId",
                schema: "app",
                table: "report_request_projects",
                columns: new[] { "ReportRequestId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_avatars_AvatarId",
                schema: "app",
                table: "user_avatars",
                column: "AvatarId");

            migrationBuilder.AddForeignKey(
                name: "FK_physical_buttons_rooms_RoomId",
                schema: "app",
                table: "physical_buttons",
                column: "RoomId",
                principalSchema: "app",
                principalTable: "rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_report_requests_users_CreatedBy",
                schema: "app",
                table: "report_requests",
                column: "CreatedBy",
                principalSchema: "app",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_physical_buttons_rooms_RoomId",
                schema: "app",
                table: "physical_buttons");

            migrationBuilder.DropForeignKey(
                name: "FK_report_requests_users_CreatedBy",
                schema: "app",
                table: "report_requests");

            migrationBuilder.DropTable(
                name: "bug_reports",
                schema: "app");

            migrationBuilder.DropTable(
                name: "button_press_logs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "monthly_score_snapshots",
                schema: "app");

            migrationBuilder.DropTable(
                name: "report_audit_logs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "report_request_projects",
                schema: "app");

            migrationBuilder.DropTable(
                name: "user_avatars",
                schema: "app");

            migrationBuilder.DropTable(
                name: "avatars",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_physical_buttons_RoomId",
                schema: "app",
                table: "physical_buttons");

            migrationBuilder.DropColumn(
                name: "ScoreAssignedAt",
                schema: "app",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ScoreCategory",
                schema: "app",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ScoreProcessed",
                schema: "app",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "app",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "app",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "app",
                table: "report_requests");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "app",
                table: "physical_buttons");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "ExitTime",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "ReaderUid",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "RfidCardId",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "app",
                table: "lab_entries");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "app",
                table: "lab_current_occupancy");

            migrationBuilder.RenameColumn(
                name: "ProfileImageUrl",
                schema: "app",
                table: "users",
                newName: "AvatarUrl");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "app",
                table: "report_requests",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_report_requests_CreatedBy",
                schema: "app",
                table: "report_requests",
                newName: "IX_report_requests_ProjectId");

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                schema: "app",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "PointsChanged",
                schema: "app",
                table: "score_history",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "RequestId",
                schema: "app",
                table: "reports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                schema: "app",
                table: "report_requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedBy",
                schema: "app",
                table: "report_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardUid",
                schema: "app",
                table: "lab_entries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryType",
                schema: "app",
                table: "lab_entries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "room_accesses",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    RfidCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DenyReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    IsAuthorized = table.Column<bool>(type: "boolean", nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_accesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_accesses_lab_entries_CreatedEntryId",
                        column: x => x.CreatedEntryId,
                        principalSchema: "app",
                        principalTable: "lab_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_room_accesses_rfid_cards_RfidCardId",
                        column: x => x.RfidCardId,
                        principalSchema: "app",
                        principalTable: "rfid_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_accesses_rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_accesses_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_requests_RequestedBy",
                schema: "app",
                table: "report_requests",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_room_accesses_CreatedEntryId",
                schema: "app",
                table: "room_accesses",
                column: "CreatedEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_room_accesses_RfidCardId",
                schema: "app",
                table: "room_accesses",
                column: "RfidCardId");

            migrationBuilder.CreateIndex(
                name: "IX_room_accesses_RoomId_AccessedAt",
                schema: "app",
                table: "room_accesses",
                columns: new[] { "RoomId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_room_accesses_UserId_AccessedAt",
                schema: "app",
                table: "room_accesses",
                columns: new[] { "UserId", "AccessedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_report_requests_projects_ProjectId",
                schema: "app",
                table: "report_requests",
                column: "ProjectId",
                principalSchema: "app",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_report_requests_users_RequestedBy",
                schema: "app",
                table: "report_requests",
                column: "RequestedBy",
                principalSchema: "app",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
