using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class LiberaModuloEtiquetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8685));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8688));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8690));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8692));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8744));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8750));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8753));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                columns: new[] { "atualizado_em", "disponivel_para_ativar", "valor" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8755), true, 29.90m });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8760));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                columns: new[] { "atualizado_em", "valor" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8762), 39.90m });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8764));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8767));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7041));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7137));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7287));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7388));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7462));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8089));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(7001));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8180));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8206));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8235));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8262));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8288));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8314));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8340));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8367));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8396));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(8424));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(6368), "$2a$11$ENN8drrMxDpVNNJBdUuf0O8nUhHnV6Q4sunPTibYGDnC3YRgTjX2e" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 42, 12, 912, DateTimeKind.Utc).AddTicks(6896));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2666));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2669));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2672));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2674));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2718));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2722));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2763));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                columns: new[] { "atualizado_em", "disponivel_para_ativar", "valor" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2766), false, 0m });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2770));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                columns: new[] { "atualizado_em", "valor" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2772), 0m });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2775));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2777));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1446));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1554));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1849));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1953));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2029));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2100));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1405));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2175));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2203));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2231));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2300));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2330));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2360));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2388));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2415));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2442));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(2469));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(757), "$2a$11$8MW6h.1pODwMa.EFfXIZ1OGtboB/kav6VXgXhKH1ZTFo.MgWc.jhK" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 7, 12, 39, 2, 752, DateTimeKind.Utc).AddTicks(1315));
        }
    }
}
