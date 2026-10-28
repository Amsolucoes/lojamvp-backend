using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261028120000_AdicionaLimitesPlanoEValorPago")]
    public partial class AdicionaLimitesPlanoEValorPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "limite_usos_mes", table: "planos", type: "integer", nullable: true);
            migrationBuilder.AddColumn<string>(name: "dias_semana_permitidos", table: "planos", type: "text", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "valor_pago", table: "pagamentos_plano", type: "numeric", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "limite_usos_mes", table: "planos");
            migrationBuilder.DropColumn(name: "dias_semana_permitidos", table: "planos");
            migrationBuilder.DropColumn(name: "valor_pago", table: "pagamentos_plano");
        }
    }
}
