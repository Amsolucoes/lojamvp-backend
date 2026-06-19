using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrocas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "credito_loja",
                table: "clientes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "trocas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_devolvido = table.Column<decimal>(type: "numeric", nullable: false),
                    total_novo = table.Column<decimal>(type: "numeric", nullable: false),
                    diferenca = table.Column<decimal>(type: "numeric", nullable: false),
                    credito_gerado = table.Column<decimal>(type: "numeric", nullable: false),
                    forma_pagamento = table.Column<string>(type: "text", nullable: true),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_trocas", x => x.id);
                    table.ForeignKey(
                        name: "f_k_trocas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itens_troca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    troca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_produto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    volta_estoque = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_troca", x => x.id);
                    table.ForeignKey(
                        name: "f_k_itens_troca__trocas_troca_id",
                        column: x => x.troca_id,
                        principalTable: "trocas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 19, 13, 46, 24, 785, DateTimeKind.Utc).AddTicks(6906));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 19, 13, 46, 24, 785, DateTimeKind.Utc).AddTicks(7016));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 19, 13, 46, 24, 785, DateTimeKind.Utc).AddTicks(7232));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 19, 13, 46, 24, 785, DateTimeKind.Utc).AddTicks(6038), "$2a$11$xR.1RAunnemELc4bo62LYeVbm4itVYeFj7Y80c.K4nqDVVnqutABe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 19, 13, 46, 24, 785, DateTimeKind.Utc).AddTicks(6827));

            migrationBuilder.CreateIndex(
                name: "i_x_itens_troca_troca_id",
                table: "itens_troca",
                column: "troca_id");

            migrationBuilder.CreateIndex(
                name: "i_x_trocas_cliente_id",
                table: "trocas",
                column: "cliente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_troca");

            migrationBuilder.DropTable(
                name: "trocas");

            migrationBuilder.DropColumn(
                name: "credito_loja",
                table: "clientes");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 13, 7, 32, 621, DateTimeKind.Utc).AddTicks(9299));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 13, 7, 32, 621, DateTimeKind.Utc).AddTicks(9425));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 13, 7, 32, 621, DateTimeKind.Utc).AddTicks(9608));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 18, 13, 7, 32, 621, DateTimeKind.Utc).AddTicks(8232), "$2a$11$ev18sHZzqkP5C10tnivaLOeHbAtJAWJ0Cfwj4gcLI7YXXj5iBXDFO" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 13, 7, 32, 621, DateTimeKind.Utc).AddTicks(9213));
        }
    }
}
