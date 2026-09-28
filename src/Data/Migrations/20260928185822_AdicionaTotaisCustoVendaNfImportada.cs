using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTotaisCustoVendaNfImportada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "quantidade_total",
                table: "nfs_importadas",
                type: "numeric(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_custo_total",
                table: "nfs_importadas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_venda_total",
                table: "nfs_importadas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(2303));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(2317));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(2326));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(2334));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3019));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3049));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3068));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3076));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3086));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3096));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3106));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3122));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(3130));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(6486));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(6716));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(8385));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(8652));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(8852));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(8997));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(9136));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(6386));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(9724));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(9805));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1176));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1257));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1333));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1386));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1441));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1490));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1542));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 474, DateTimeKind.Utc).AddTicks(1595));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(5246), "$2a$11$VGKhpA8p2IBJtS3QuwYVcuz1uyEaLPb69hSIGOZh/SPRLSkTdPLCi" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 28, 18, 58, 19, 473, DateTimeKind.Utc).AddTicks(6172));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantidade_total",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "valor_custo_total",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "valor_venda_total",
                table: "nfs_importadas");

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5740));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5751));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5758));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5765));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5852));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5864));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5878));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5885));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5896));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5903));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5911));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5920));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5926));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5933));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2985));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3174));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3413));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3567));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3732));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3857));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4388));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2154));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4532));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4582));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4624));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4668));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4719));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4761));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4801));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4842));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4879));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4918));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(1436), "$2a$11$2ya2zTODljcKGch9OoXujOkNOqaO/R84vEyqFb0TiP9kqlWJLLKAa" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2022));
        }
    }
}
