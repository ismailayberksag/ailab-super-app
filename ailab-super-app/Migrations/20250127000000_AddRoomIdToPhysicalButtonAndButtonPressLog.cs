using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomIdToPhysicalButtonAndButtonPressLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RoomId column to physical_buttons table
            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                schema: "app",
                table: "physical_buttons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Add foreign key constraint for RoomId
            migrationBuilder.AddForeignKey(
                name: "FK_physical_buttons_rooms_RoomId",
                schema: "app",
                table: "physical_buttons",
                column: "RoomId",
                principalSchema: "app",
                principalTable: "rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Create button_press_logs table
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

            // Create indexes for button_press_logs
            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_ButtonId",
                schema: "app",
                table: "button_press_logs",
                column: "ButtonId");

            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_RoomId",
                schema: "app",
                table: "button_press_logs",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_button_press_logs_PressedAt",
                schema: "app",
                table: "button_press_logs",
                column: "PressedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop button_press_logs table
            migrationBuilder.DropTable(
                name: "button_press_logs",
                schema: "app");

            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_physical_buttons_rooms_RoomId",
                schema: "app",
                table: "physical_buttons");

            // Drop RoomId column from physical_buttons table
            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "app",
                table: "physical_buttons");
        }
    }
}

