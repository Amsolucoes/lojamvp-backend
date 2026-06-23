using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class CamposPromocaoLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "meses_promocional",
                table: "lojas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "promocional",
                table: "lojas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_pos_promocional",
                table: "lojas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_promocional",
                table: "lojas",
                type: "numeric(10,2)",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "meses_promocional",
                table: "lojas");

            migrationBuilder.DropColumn(
                name: "promocional",
                table: "lojas");

            migrationBuilder.DropColumn(
                name: "valor_pos_promocional",
                table: "lojas");

            migrationBuilder.DropColumn(
                name: "valor_promocional",
                table: "lojas");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 14, 50, 2, 718, DateTimeKind.Utc).AddTicks(3591));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 14, 50, 2, 718, DateTimeKind.Utc).AddTicks(3692));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 14, 50, 2, 718, DateTimeKind.Utc).AddTicks(3822));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 23, 14, 50, 2, 718, DateTimeKind.Utc).AddTicks(2635), "$2a$11$WRSFIFgYpCVZBwkjvvVcg.PQmPF62QoUDssXA7yp6OcNM9f6GucpW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 23, 14, 50, 2, 718, DateTimeKind.Utc).AddTicks(3504));
        }
    }
}
