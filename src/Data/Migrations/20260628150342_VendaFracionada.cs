using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class VendaFracionada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "estoque_minimo",
                table: "produtos",
                type: "numeric(10,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "estoque",
                table: "produtos",
                type: "numeric(10,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "tipo_venda",
                table: "produtos",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unidade_medida",
                table: "produtos",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantidade",
                table: "movimentos",
                type: "numeric(10,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantidade",
                table: "itens_venda",
                type: "numeric(10,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 3, 41, 560, DateTimeKind.Utc).AddTicks(4342));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 3, 41, 560, DateTimeKind.Utc).AddTicks(4477));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 3, 41, 560, DateTimeKind.Utc).AddTicks(4633));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 28, 15, 3, 41, 560, DateTimeKind.Utc).AddTicks(2969), "$2a$11$AoY54aYBp5ltsCiuNpJMpuKiiwwVkYV6unRyvPdvbQFp/UhDJ33fW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 3, 41, 560, DateTimeKind.Utc).AddTicks(4223));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo_venda",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "unidade_medida",
                table: "produtos");

            migrationBuilder.AlterColumn<int>(
                name: "estoque_minimo",
                table: "produtos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)");

            migrationBuilder.AlterColumn<int>(
                name: "estoque",
                table: "produtos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)");

            migrationBuilder.AlterColumn<int>(
                name: "quantidade",
                table: "movimentos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)");

            migrationBuilder.AlterColumn<int>(
                name: "quantidade",
                table: "itens_venda",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 25, 2, 18, 23, 188, DateTimeKind.Utc).AddTicks(6942));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 25, 2, 18, 23, 188, DateTimeKind.Utc).AddTicks(7030));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 25, 2, 18, 23, 188, DateTimeKind.Utc).AddTicks(7155));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 25, 2, 18, 23, 188, DateTimeKind.Utc).AddTicks(6001), "$2a$11$BavNMRmvMkeCxiSxYSFtGOoNVgsuc9xX5DqaWqyH509oU19Cf1JU." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 25, 2, 18, 23, 188, DateTimeKind.Utc).AddTicks(6851));
        }
    }
}
