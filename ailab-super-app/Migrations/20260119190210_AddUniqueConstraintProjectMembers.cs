using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ailab_super_app.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintProjectMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Önce duplicate kayıtları temizle (Varsa)
            // Aynı projede aynı user birden fazla kez varsa, en eski kaydı (MIN Id) tutup diğerlerini siliyoruz.
            // SQLite/SQLServer uyumlu generic yapı kurmaya çalışalım ama raw SQL en temizi.
            // Aşağıdaki SQL Server (MSSQL) içindir, proje MSSQL kullanıyorsa çalışır.
            migrationBuilder.Sql(@"
                WITH Duplicates AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY ProjectId, UserId ORDER BY AddedAt) AS RowNum
                    FROM ProjectMembers
                )
                DELETE FROM ProjectMembers
                WHERE Id IN (
                    SELECT Id FROM Duplicates WHERE RowNum > 1
                );
            ");

            // 2. Unique Index Ekle
            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers");
        }
    }
}