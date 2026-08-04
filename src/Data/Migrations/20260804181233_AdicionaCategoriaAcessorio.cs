using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCategoriaAcessorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_acessorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    chave = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_categorias_acessorio", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "categorias_acessorio",
                columns: new[] { "id", "ativa", "chave", "criado_em", "nome", "ordem" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), true, "leitor_codigo_barras", new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2820), "Leitor de código de barras", 0 },
                    { new Guid("22222222-2222-2222-2222-222222222202"), true, "impressora_fiscal", new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2823), "Impressora fiscal", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222203"), true, "impressora_etiquetas", new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2825), "Impressora de etiquetas", 2 },
                    { new Guid("22222222-2222-2222-2222-222222222204"), true, "outro", new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2827), "Outro", 3 }
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2872));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2876));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2880));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2882));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2887));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2890));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2892));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2894));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1684));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1771));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1911));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2008));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2079));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2198));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1644));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2273));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2301));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2330));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2356));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2383));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2410));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2437));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2463));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2492));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(2519));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1092), "$2a$11$Ke4meo7C5VdMAXGkMp/eQ.yrq9xTcs50POYt5unqMcKaCBEAru4GW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 18, 12, 32, 158, DateTimeKind.Utc).AddTicks(1568));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias_acessorio");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1685));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1688));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1693));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1695));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1697));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1700));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1702));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1704));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(389));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(489));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(642));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(923));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1012));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1091));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(343));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1172));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1204));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1237));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1266));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1296));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1326));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1355));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1430));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1464));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(1495));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 4, 17, 49, 1, 341, DateTimeKind.Utc).AddTicks(9710), "$2a$11$l50UnINTyQRxrFMLLXSO5unTyaydyaehNLfUTDzvrpKAOQfIE/p1S" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 4, 17, 49, 1, 342, DateTimeKind.Utc).AddTicks(243));
        }
    }
}
