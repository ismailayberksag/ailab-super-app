using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddRfidReaderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RfidReaders",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReaderUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidReaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfidReaders_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidReaders_RoomId",
                schema: "app",
                table: "RfidReaders",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RfidReaders_ReaderUid",
                schema: "app",
                table: "RfidReaders",
                column: "ReaderUid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RfidReaders",
                schema: "app");
        }
    }
}
