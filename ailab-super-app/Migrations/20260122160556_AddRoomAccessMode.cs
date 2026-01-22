using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomAccessMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessMode",
                schema: "app",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessMode",
                schema: "app",
                table: "rooms");
        }
    }
}
