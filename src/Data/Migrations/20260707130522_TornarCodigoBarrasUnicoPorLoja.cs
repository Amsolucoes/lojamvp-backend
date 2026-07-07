using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class TornarCodigoBarrasUnicoPorLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_produtos_codigo_barras",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "i_x_produtos_loja_id",
                table: "produtos");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8097));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8193));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8313));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8409));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8598));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8668));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8051));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8738));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8764));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8792));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8821));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8851));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(8879));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(7167), "$2a$11$cl7hZcO0SuiqlO.cRXh6g.Hs5Et5DE2HC69UrcWPAQ8D.W2OOB2IG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 5, 21, 951, DateTimeKind.Utc).AddTicks(7971));

            migrationBuilder.CreateIndex(
                name: "i_x_produtos_loja_id_codigo_barras",
                table: "produtos",
                columns: new[] { "loja_id", "codigo_barras" },
                unique: true,
                filter: "codigo_barras IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_produtos_loja_id_codigo_barras",
                table: "produtos");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8506));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8588));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8723));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8916));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8992));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9064));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8461));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9138));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9166));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9224));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9253));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(9282));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(7602), "$2a$11$lODgAfI41TlHofeX0bTEVeb4tOszEOujf4KamXfVTCMfrI.BSTiWK" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 6, 22, 57, 28, 905, DateTimeKind.Utc).AddTicks(8378));

            migrationBuilder.CreateIndex(
                name: "i_x_produtos_codigo_barras",
                table: "produtos",
                column: "codigo_barras",
                unique: true,
                filter: "codigo_barras IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_produtos_loja_id",
                table: "produtos",
                column: "loja_id");
        }
    }
}
