using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowRide.Inicializador.Migraciones
{
    /// <inheritdoc />
    public partial class CargaMensualItinerarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "domicilio",
                schema: "ops",
                table: "pasajeros",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pasajero_id",
                schema: "ops",
                table: "itinerarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_itinerarios_pasajero_id_codigo_servicio_fecha",
                schema: "ops",
                table: "itinerarios",
                columns: new[] { "pasajero_id", "codigo_servicio", "fecha" },
                unique: true,
                filter: "pasajero_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_itinerarios_pasajero_id_codigo_servicio_fecha",
                schema: "ops",
                table: "itinerarios");

            migrationBuilder.DropColumn(
                name: "domicilio",
                schema: "ops",
                table: "pasajeros");

            migrationBuilder.DropColumn(
                name: "pasajero_id",
                schema: "ops",
                table: "itinerarios");
        }
    }
}
