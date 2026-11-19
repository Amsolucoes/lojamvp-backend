using System;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261119120000_AdicionaBonificacao")]
    public partial class AdicionaBonificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "bonificacao", table: "itens_venda", type: "boolean", nullable: false, defaultValue: false);

            migrationBuilder.CreateTable(
                name: "bonificacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome_cliente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome_produto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    venda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estornada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_bonificacoes", x => x.id);
                    table.ForeignKey(name: "f_k_bonificacoes_clientes_cliente_id", column: x => x.cliente_id, principalTable: "clientes", principalColumn: "id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "f_k_bonificacoes_lojas_loja_id", column: x => x.loja_id, principalTable: "lojas", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "f_k_bonificacoes_produtos_produto_id", column: x => x.produto_id, principalTable: "produtos", principalColumn: "id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "f_k_bonificacoes_vendas_venda_id", column: x => x.venda_id, principalTable: "vendas", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "i_x_bonificacoes_cliente_id", table: "bonificacoes", column: "cliente_id");
            migrationBuilder.CreateIndex(name: "i_x_bonificacoes_loja_id_cliente_id", table: "bonificacoes", columns: new[] { "loja_id", "cliente_id" });
            migrationBuilder.CreateIndex(name: "i_x_bonificacoes_loja_id_criado_em", table: "bonificacoes", columns: new[] { "loja_id", "criado_em" });
            migrationBuilder.CreateIndex(name: "i_x_bonificacoes_produto_id", table: "bonificacoes", column: "produto_id");
            migrationBuilder.CreateIndex(name: "i_x_bonificacoes_venda_id", table: "bonificacoes", column: "venda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bonificacoes");
            migrationBuilder.DropColumn(name: "bonificacao", table: "itens_venda");
        }
    }
}
