using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261030120000_AdicionaCobrancaEmailPagamento")]
    public partial class AdicionaCobrancaEmailPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "ultimo_aviso_cobranca_em", table: "pagamentos", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "cobranca_mp_payment_id", table: "pagamentos", type: "character varying(100)", maxLength: 100, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ultimo_aviso_cobranca_em", table: "pagamentos");
            migrationBuilder.DropColumn(name: "cobranca_mp_payment_id", table: "pagamentos");
        }
    }
}
