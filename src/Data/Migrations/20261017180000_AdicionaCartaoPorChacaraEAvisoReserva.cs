using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261017180000_AdicionaCartaoPorChacaraEAvisoReserva")]
    public partial class AdicionaCartaoPorChacaraEAvisoReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(name: "percentual_taxa_cartao", table: "configuracoes_preco_chacara", type: "numeric", nullable: false, defaultValue: 5m);
            migrationBuilder.AddColumn<decimal>(name: "valor_limite_parcelas", table: "configuracoes_preco_chacara", type: "numeric", nullable: false, defaultValue: 800m);
            migrationBuilder.AddColumn<int>(name: "parcelas_ate_limite", table: "configuracoes_preco_chacara", type: "integer", nullable: false, defaultValue: 2);
            migrationBuilder.AddColumn<int>(name: "parcelas_acima_limite", table: "configuracoes_preco_chacara", type: "integer", nullable: false, defaultValue: 3);
            migrationBuilder.AddColumn<bool>(name: "aviso_expiracao_enviado", table: "reservas", type: "boolean", nullable: false, defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "percentual_taxa_cartao", table: "configuracoes_preco_chacara");
            migrationBuilder.DropColumn(name: "valor_limite_parcelas", table: "configuracoes_preco_chacara");
            migrationBuilder.DropColumn(name: "parcelas_ate_limite", table: "configuracoes_preco_chacara");
            migrationBuilder.DropColumn(name: "parcelas_acima_limite", table: "configuracoes_preco_chacara");
            migrationBuilder.DropColumn(name: "aviso_expiracao_enviado", table: "reservas");
        }
    }
}
