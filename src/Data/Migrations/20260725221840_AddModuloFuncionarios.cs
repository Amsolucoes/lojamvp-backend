using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloFuncionarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2515));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2518));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2522));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2524));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2526));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2529));

            migrationBuilder.InsertData(
                table: "modulos_preco",
                columns: new[] { "id", "atualizado_em", "chave", "disponivel_para_ativar", "nome", "valor" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111107"), new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2531), "funcionarios", true, "Funcionários (comissão e pagamento)", 39.90m });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1519));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1613));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1736));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1821));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1988));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2051));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1481));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2113));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2136));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2160));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2183));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2205));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2228));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2251));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2276));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2301));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(2326));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(844), "$2a$11$sOAJ7nPcW9FTT1Ln1BQZrucH/0SV1AqsOI6LdjUa0B.CL0ivs9oYC" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 22, 18, 39, 351, DateTimeKind.Utc).AddTicks(1399));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2271));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2276));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2283));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2286));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2289));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2292));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1339));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1450));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1530));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1608));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(887));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1745));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1811));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1842));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1872));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1903));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1934));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1964));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1993));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2022));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 25, 21, 41, 54, 424, DateTimeKind.Utc).AddTicks(9890), "$2a$11$P9GsP9A6X6L/7assdKXUHeD8ahr2ikmV4b70T4N67Wa1Utp06apYe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(755));
        }
    }
}
