using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mensajes.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mensajeria");

            migrationBuilder.CreateTable(
                name: "conversaciones",
                schema: "mensajeria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Usuario1Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Usuario2Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UltimaActividad = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoMensajeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mensajes",
                schema: "mensajeria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RemitenteId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinatarioId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GrupoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Contenido = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    TipoMensaje = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leido = table.Column<bool>(type: "boolean", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversaciones_UltimaActividad",
                schema: "mensajeria",
                table: "conversaciones",
                column: "UltimaActividad");

            migrationBuilder.CreateIndex(
                name: "IX_conversaciones_Usuario1Id_Usuario2Id",
                schema: "mensajeria",
                table: "conversaciones",
                columns: new[] { "Usuario1Id", "Usuario2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_DestinatarioId",
                schema: "mensajeria",
                table: "mensajes",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_FechaEnvio",
                schema: "mensajeria",
                table: "mensajes",
                column: "FechaEnvio");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_GrupoId",
                schema: "mensajeria",
                table: "mensajes",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_RemitenteId",
                schema: "mensajeria",
                table: "mensajes",
                column: "RemitenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversaciones",
                schema: "mensajeria");

            migrationBuilder.DropTable(
                name: "mensajes",
                schema: "mensajeria");
        }
    }
}
