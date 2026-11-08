using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonajesApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNotaPrivadaCifrada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotaPrivadaCifrada",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotaPrivadaCifrada",
                table: "Usuarios");
        }
    }
}
