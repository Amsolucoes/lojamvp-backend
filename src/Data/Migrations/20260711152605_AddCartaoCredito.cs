using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCartaoCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cartoes_credito",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    limite = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    dia_fechamento = table.Column<int>(type: "integer", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    conta_bancaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_cartoes_credito", x => x.id);
                    table.ForeignKey(
                        name: "f_k_cartoes_credito__contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faturas_cartao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_credito_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mes_referencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_faturas_cartao", x => x.id);
                    table.ForeignKey(
                        name: "f_k_faturas_cartao_cartoes_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartoes_credito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos_cartao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_credito_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    data_compra = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_lancamentos_cartao", x => x.id);
                    table.ForeignKey(
                        name: "f_k_lancamentos_cartao_cartoes_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartoes_credito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_lancamentos_cartao_categorias_financeiras_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_financeiras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8351));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8463));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8613));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8716));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8794));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8914));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8303));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8994));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(9044));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(9075));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(9104));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(9134));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(9174));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(7382), "$2a$11$/bqQPk6gs8Ncp9wlYfN38eL1TypW1ftOcoCrEYN7znVv6.NspNjDy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 15, 26, 4, 649, DateTimeKind.Utc).AddTicks(8187));

            migrationBuilder.CreateIndex(
                name: "i_x_cartoes_credito_conta_bancaria_id",
                table: "cartoes_credito",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_faturas_cartao_cartao_credito_id",
                table: "faturas_cartao",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_cartao_cartao_credito_id",
                table: "lancamentos_cartao",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_cartao_categoria_id",
                table: "lancamentos_cartao",
                column: "categoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faturas_cartao");

            migrationBuilder.DropTable(
                name: "lancamentos_cartao");

            migrationBuilder.DropTable(
                name: "cartoes_credito");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8603));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8704));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8841));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9015));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9086));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9302));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8560));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9437));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9470));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9500));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9529));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9557));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9584));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(7826), "$2a$11$t6vOay27OrqORDaHYGcOguQ/o3ofijHVRxijNUYpwWoBVM7K8AAfW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8469));
        }
    }
}
