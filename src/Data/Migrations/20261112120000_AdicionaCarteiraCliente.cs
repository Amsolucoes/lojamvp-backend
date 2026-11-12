using System;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261112120000_AdicionaCarteiraCliente")]
    public partial class AdicionaCarteiraCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(name: "limite_carteira", table: "clientes", type: "numeric(10,2)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "valor_carteira", table: "vendas", type: "numeric(10,2)", nullable: true);

            migrationBuilder.CreateTable(
                name: "movimentos_carteira",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    venda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forma_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    movimento_caixa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_movimentos_carteira", x => x.id);
                    table.ForeignKey(name: "f_k_movimentos_carteira_clientes_cliente_id", column: x => x.cliente_id, principalTable: "clientes", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "f_k_movimentos_carteira_lojas_loja_id", column: x => x.loja_id, principalTable: "lojas", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "f_k_movimentos_carteira_vendas_venda_id", column: x => x.venda_id, principalTable: "vendas", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "i_x_movimentos_carteira_cliente_id", table: "movimentos_carteira", column: "cliente_id");
            migrationBuilder.CreateIndex(name: "i_x_movimentos_carteira_loja_id_cliente_id", table: "movimentos_carteira", columns: new[] { "loja_id", "cliente_id" });
            migrationBuilder.CreateIndex(name: "i_x_movimentos_carteira_venda_id", table: "movimentos_carteira", column: "venda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "movimentos_carteira");
            migrationBuilder.DropColumn(name: "limite_carteira", table: "clientes");
            migrationBuilder.DropColumn(name: "valor_carteira", table: "vendas");
        }
    }
}
