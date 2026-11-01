using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowRide.Inicializador.Migraciones
{
    /// <inheritdoc />
    public partial class CargaItinerariosBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "archivo_itinerario",
                schema: "ops",
                table: "conversaciones",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archivo_itinerario",
                schema: "ops",
                table: "conversaciones");
        }
    }
}
