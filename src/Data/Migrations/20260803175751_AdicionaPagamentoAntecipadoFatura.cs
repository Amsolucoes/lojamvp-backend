using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPagamentoAntecipadoFatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pagamentos_antecipados_fatura",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fatura_cartao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_bancaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_pagamentos_antecipados_fatura", x => x.id);
                    table.ForeignKey(
                        name: "f_k_pagamentos_antecipados_fatura_contas_bancarias_conta_bancar~",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_pagamentos_antecipados_fatura_faturas_cartao_fatura_cartao_~",
                        column: x => x.fatura_cartao_id,
                        principalTable: "faturas_cartao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7767));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7770));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7774));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7778));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7780));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7782));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7785));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(6678));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(6779));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7008));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7093));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7155));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7238));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(6641));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7302));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7340));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7364));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7438));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7461));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7497));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7521));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7544));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7568));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(7590));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(5989), "$2a$11$is1ZBFBRxtcJMk5j9L/NqOqrDysHxgiDU/47K0n1xwvkOPCTypGoK" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 57, 50, 598, DateTimeKind.Utc).AddTicks(6560));

            migrationBuilder.CreateIndex(
                name: "i_x_pagamentos_antecipados_fatura_conta_bancaria_id",
                table: "pagamentos_antecipados_fatura",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_pagamentos_antecipados_fatura_fatura_cartao_id",
                table: "pagamentos_antecipados_fatura",
                column: "fatura_cartao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagamentos_antecipados_fatura");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7623));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7627));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7631));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7633));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7636));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7638));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7640));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6470));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6563));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6712));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6919));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6994));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7064));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6428));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7139));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7169));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7197));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7226));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7254));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7282));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7343));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7373));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7403));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(7431));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(5825), "$2a$11$QFjtxKOGBp7IyHPZiv/QHOQLP7NiyFhnOCnhymW40DKEZnxvtf5cu" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 3, 17, 49, 49, 133, DateTimeKind.Utc).AddTicks(6336));
        }
    }
}
