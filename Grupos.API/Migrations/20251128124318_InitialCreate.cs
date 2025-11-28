using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupos.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "grupos");

            migrationBuilder.CreateTable(
                name: "Grupos",
                schema: "grupos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MiembrosGrupo",
                schema: "grupos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiembrosGrupo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupo_GrupoId_UsuarioId",
                schema: "grupos",
                table: "MiembrosGrupo",
                columns: new[] { "GrupoId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grupos",
                schema: "grupos");

            migrationBuilder.DropTable(
                name: "MiembrosGrupo",
                schema: "grupos");
        }
    }
}
