using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloFinanceiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contas_bancarias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_contas_bancarias", x => x.id);
                    table.ForeignKey(
                        name: "f_k_contas_bancarias__lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_conta_bancaria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_bancaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_ajustes_conta_bancaria", x => x.id);
                    table.ForeignKey(
                        name: "f_k_ajustes_conta_bancaria__contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos_financeiros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_bancaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    modo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    grupo_parcelamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    numero_parcela = table.Column<int>(type: "integer", nullable: true),
                    total_parcelas = table.Column<int>(type: "integer", nullable: true),
                    lancamento_fixo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_lancamentos_financeiros", x => x.id);
                    table.ForeignKey(
                        name: "f_k_lancamentos_financeiros_contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos_fixos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_bancaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_lancamentos_fixos", x => x.id);
                    table.ForeignKey(
                        name: "f_k_lancamentos_fixos_contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7977));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8084));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8223));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8438));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8517));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8594));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7932));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8674));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8705));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8736));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8766));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8829));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8859));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7069), "$2a$11$bVH7mLGB.MZbXEzEST3wRufffk6lV.XS.zmBEkUoOKPrJmle5aviy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7837));

            migrationBuilder.CreateIndex(
                name: "i_x_ajustes_conta_bancaria_conta_bancaria_id",
                table: "ajustes_conta_bancaria",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_contas_bancarias_loja_id",
                table: "contas_bancarias",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_financeiros_conta_bancaria_id",
                table: "lancamentos_financeiros",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_fixos_conta_bancaria_id",
                table: "lancamentos_fixos",
                column: "conta_bancaria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ajustes_conta_bancaria");

            migrationBuilder.DropTable(
                name: "lancamentos_financeiros");

            migrationBuilder.DropTable(
                name: "lancamentos_fixos");

            migrationBuilder.DropTable(
                name: "contas_bancarias");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4608));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4765));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4893));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5047));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5115));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5187));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4557));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5304));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5335));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5364));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5406));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5444));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5474));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(3876), "$2a$11$1uZrMLm8/R9JEIQvEWYHX.7.gCea4xKnz5Lz3jJqfpHIzTzgqJ8JS" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4463));
        }
    }
}
