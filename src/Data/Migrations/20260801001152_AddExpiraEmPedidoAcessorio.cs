using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiraEmPedidoAcessorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "expira_em",
                table: "pedidos_acessorio",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6466));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6470));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6474));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6476));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6480));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6483));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6485));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5334));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5448));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5586));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5690));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5763));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5836));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5101));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5994));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6021));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6051));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6081));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6109));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6136));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6162));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6186));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6212));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(6241));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(4152), "$2a$11$UnLWUNR8Jng7YC5tpJT0POTjcbGubblv6gHWa.Eg3Q9KHtvfBXetG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 1, 0, 11, 51, 141, DateTimeKind.Utc).AddTicks(5002));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expira_em",
                table: "pedidos_acessorio");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1781));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1785));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1787));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1791));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1793));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1796));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(652));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(775));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(983));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1176));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1236));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(613));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1296));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1318));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1340));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1361));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1402));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1445));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1466));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1488));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1563));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(5), "$2a$11$wiyCWTYt9mrZPcLOYyK4Xej8trKcqdpUavNSLLzMFTJ.G9ObTQo5y" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(528));
        }
    }
}
