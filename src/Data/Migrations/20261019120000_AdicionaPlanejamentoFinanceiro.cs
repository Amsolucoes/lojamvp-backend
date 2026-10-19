using System;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261019120000_AdicionaPlanejamentoFinanceiro")]
    public partial class AdicionaPlanejamentoFinanceiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planejamentos_financeiros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receita_planejada = table.Column<decimal>(type: "numeric", nullable: true),
                    percentual_poupanca = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table => { table.PrimaryKey("p_k_planejamentos_financeiros", x => x.id); });

            migrationBuilder.CreateIndex(
                name: "i_x_planejamentos_financeiros_loja_id",
                table: "planejamentos_financeiros",
                column: "loja_id",
                unique: true);

            migrationBuilder.CreateTable(
                name: "planejamento_categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => { table.PrimaryKey("p_k_planejamento_categorias", x => x.id); });

            migrationBuilder.CreateIndex(
                name: "i_x_planejamento_categorias_loja_id_categoria_id",
                table: "planejamento_categorias",
                columns: new[] { "loja_id", "categoria_id" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "metas_planejamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    icone = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    valor_alvo = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_guardado = table.Column<decimal>(type: "numeric", nullable: false),
                    data_alvo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("p_k_metas_planejamento", x => x.id); });

            migrationBuilder.CreateIndex(
                name: "i_x_metas_planejamento_loja_id",
                table: "metas_planejamento",
                column: "loja_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "planejamentos_financeiros");
            migrationBuilder.DropTable(name: "planejamento_categorias");
            migrationBuilder.DropTable(name: "metas_planejamento");
        }
    }
}
