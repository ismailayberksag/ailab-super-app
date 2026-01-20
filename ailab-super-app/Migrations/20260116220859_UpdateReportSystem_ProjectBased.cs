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

            /* TABLOLAR ZATEN VAR, TEKRAR OLUŞTURMA (Manual Fix)
            migrationBuilder.CreateTable(
                name: "avatars",
                ...
            migrationBuilder.CreateTable(
                name: "bug_reports",
                ...
            ... tüm create table'lar ...
            */

            /* INDEX VE FK'LAR DA ZATEN VAR (Manual Fix)
            migrationBuilder.CreateIndex(
                name: "IX_physical_buttons_RoomId",
                ...
            ...
            migrationBuilder.AddForeignKey(
                name: "FK_report_requests_users_CreatedBy",
                ...
            */
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
