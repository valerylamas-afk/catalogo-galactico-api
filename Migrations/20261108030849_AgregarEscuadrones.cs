using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonajesApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEscuadrones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Escuadrones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Faccion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    LiderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escuadrones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escuadrones_Personajes_LiderId",
                        column: x => x.LiderId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EscuadronIntegrantes",
                columns: table => new
                {
                    EscuadronId = table.Column<int>(type: "int", nullable: false),
                    PersonajeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscuadronIntegrantes", x => new { x.EscuadronId, x.PersonajeId });
                    table.ForeignKey(
                        name: "FK_EscuadronIntegrantes_Escuadrones_EscuadronId",
                        column: x => x.EscuadronId,
                        principalTable: "Escuadrones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscuadronIntegrantes_Personajes_PersonajeId",
                        column: x => x.PersonajeId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Escuadrones_LiderId",
                table: "Escuadrones",
                column: "LiderId");

            migrationBuilder.CreateIndex(
                name: "IX_EscuadronIntegrantes_PersonajeId",
                table: "EscuadronIntegrantes",
                column: "PersonajeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscuadronIntegrantes");

            migrationBuilder.DropTable(
                name: "Escuadrones");
        }
    }
}
