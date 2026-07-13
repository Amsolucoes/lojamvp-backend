using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloPreco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modulos_preco",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    disponivel_para_ativar = table.Column<bool>(type: "boolean", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_modulos_preco", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "modulos_preco",
                columns: new[] { "id", "atualizado_em", "chave", "disponivel_para_ativar", "nome", "valor" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8872), "servicos", true, "Serviços e Agenda", 0m },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8875), "financeiro", true, "Financeiro (Contas a Pagar/Receber)", 29.90m },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8879), "turmas", true, "Turmas (aulas em grupo)", 39.90m },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8884), "etiquetas", false, "Impressão de etiquetas", 0m },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8886), "nf", false, "Importação de NF", 0m }
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8014));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8104));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8232));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8317));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8381));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8490));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(7976));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8556));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8579));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8603));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8625));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8648));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(8671));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(7403), "$2a$11$R4e0dOi.IdKvuW6QgTPFvuG4KQ5LbhxXEfvsXeXEBtaOYnb9cgQPm" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 48, 27, 930, DateTimeKind.Utc).AddTicks(7903));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modulos_preco");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3316));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3413));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3546));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3640));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3792));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3937));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3274));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4027));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4052));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4091));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4116));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4142));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(4166));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(2471), "$2a$11$5Y.m.EG/ZpdUtb1eP/rr2us81H0gWyqi1Us2icgQa9Te1GBW.KCeG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 13, 21, 23, 36, 306, DateTimeKind.Utc).AddTicks(3037));
        }
    }
}
