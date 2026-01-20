using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddFirebaseAuthenticationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthProvider",
                schema: "app",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FirebaseUid",
                schema: "app",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedToFirebaseAt",
                schema: "app",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Manuel Ekleme: FirebaseUid unique olmalı (ama null değerlere izin verilmeli)
            migrationBuilder.CreateIndex(
                name: "IX_Users_FirebaseUid",
                schema: "app",
                table: "users",
                column: "FirebaseUid",
                unique: true,
                filter: "\"FirebaseUid\" IS NOT NULL"); // PostgreSQL syntax
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FirebaseUid",
                schema: "app",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                schema: "app",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FirebaseUid",
                schema: "app",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MigratedToFirebaseAt",
                schema: "app",
                table: "users");
        }
    }
}
