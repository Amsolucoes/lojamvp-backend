using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigGradeCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tamanhos_personalizados",
                table: "categorias_loja",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "usa_cor",
                table: "categorias_loja",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "usa_tamanho",
                table: "categorias_loja",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tamanhos_personalizados",
                table: "categorias_loja");

            migrationBuilder.DropColumn(
                name: "usa_cor",
                table: "categorias_loja");

            migrationBuilder.DropColumn(
                name: "usa_tamanho",
                table: "categorias_loja");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 19, 53, 53, 867, DateTimeKind.Utc).AddTicks(1171));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 19, 53, 53, 867, DateTimeKind.Utc).AddTicks(1273));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 19, 53, 53, 867, DateTimeKind.Utc).AddTicks(1404));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 23, 19, 53, 53, 867, DateTimeKind.Utc).AddTicks(419), "$2a$11$eq5tUY3GiFTi04YTFeOnk.D1gDNuxlQ/YEkfBVIOesUNrVEaeEBS2" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 19, 53, 53, 867, DateTimeKind.Utc).AddTicks(1093));
        }
    }
}
