using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Encuesta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRespuestas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Respuesta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncuestaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Huella = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EnviadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respuesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Respuesta_Encuesta_EncuestaId",
                        column: x => x.EncuestaId,
                        principalTable: "Encuesta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemRespuesta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RespuestaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRespuesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRespuesta_Respuesta_RespuestaId",
                        column: x => x.RespuestaId,
                        principalTable: "Respuesta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemRespuesta_RespuestaId",
                table: "ItemRespuesta",
                column: "RespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Respuesta_EncuestaId_Huella",
                table: "Respuesta",
                columns: new[] { "EncuestaId", "Huella" },
                unique: true,
                filter: "[Huella] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemRespuesta");

            migrationBuilder.DropTable(
                name: "Respuesta");
        }
    }
}
