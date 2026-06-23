using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class SignupEFixBermuda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "numero");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "nummero");

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
        }
    }
}
