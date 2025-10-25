using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddDoorStateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lab_current_occupancy_users_UserId1",
                schema: "app",
                table: "lab_current_occupancy");

            migrationBuilder.DropIndex(
                name: "IX_lab_current_occupancy_UserId1",
                schema: "app",
                table: "lab_current_occupancy");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "app",
                table: "lab_current_occupancy");

            migrationBuilder.CreateTable(
                name: "door_states",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_door_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_door_states_rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfid_readers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReaderUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfid_readers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfid_readers_rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_SchoolNumber",
                schema: "app",
                table: "users",
                column: "SchoolNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_door_states_RoomId",
                schema: "app",
                table: "door_states",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfid_readers_ReaderUid",
                schema: "app",
                table: "rfid_readers",
                column: "ReaderUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfid_readers_RoomId_Location",
                schema: "app",
                table: "rfid_readers",
                columns: new[] { "RoomId", "Location" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "door_states",
                schema: "app");

            migrationBuilder.DropTable(
                name: "rfid_readers",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_users_SchoolNumber",
                schema: "app",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "app",
                table: "lab_current_occupancy",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_lab_current_occupancy_UserId1",
                schema: "app",
                table: "lab_current_occupancy",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_lab_current_occupancy_users_UserId1",
                schema: "app",
                table: "lab_current_occupancy",
                column: "UserId1",
                principalSchema: "app",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
