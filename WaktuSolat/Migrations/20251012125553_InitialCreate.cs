using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaktuSolat.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaktuSolat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    czone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    cbearing = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TarikhMasehi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TarikhHijrah = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Imsak = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Subuh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Syuruk = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Dhuha = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Zohor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Asar = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Maghrib = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Isyak = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaktuSolat", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaktuSolat");
        }
    }
}
