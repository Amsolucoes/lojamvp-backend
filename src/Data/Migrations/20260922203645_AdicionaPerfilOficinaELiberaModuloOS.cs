using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPerfilOficinaELiberaModuloOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4180));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4192));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4199));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4205));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4327));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4340));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4348));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4355));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4361));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4369));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4377));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4385));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                columns: new[] { "atualizado_em", "disponivel_para_ativar" },
                values: new object[] { new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4392), true });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(4399));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(1494));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(1652));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(1891));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2046));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2537));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2665));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(1423));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2904));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2944));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2982));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3023));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3084));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3121));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3160));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3274));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3315));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(3687));

            migrationBuilder.InsertData(
                table: "perfis_loja",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "icone", "nome", "tipo_plano_aplica" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000007"), true, new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(2793), "Para oficinas mecânicas e lojas de autopeças, pneus e acessórios", "🔧", "Oficina / Auto Peças", "loja" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(659), "$2a$11$4p3uE4OnQRFvMKlfH06LBOK.BPXkDc5Eu1XJtGRh.Pb3p9mtK3dSS" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 22, 20, 36, 43, 851, DateTimeKind.Utc).AddTicks(1277));

            migrationBuilder.InsertData(
                table: "categorias_perfil_loja",
                columns: new[] { "id", "nome", "ordem", "perfil_loja_id", "tipo_tamanho" },
                values: new object[,]
                {
                    { new Guid("19000000-0000-0000-0000-000000000001"), "Peças e Autopeças", 0, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000002"), "Óleo e Lubrificantes", 1, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000003"), "Pneus e Rodas", 2, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000004"), "Baterias", 3, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000005"), "Acessórios", 4, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000006"), "Ferramentas", 5, new Guid("10000000-0000-0000-0000-000000000007"), "letra" },
                    { new Guid("19000000-0000-0000-0000-000000000007"), "Outro", 6, new Guid("10000000-0000-0000-0000-000000000007"), "letra" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("19000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4252));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4265));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4274));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4284));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4365));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4379));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4391));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4398));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4405));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4413));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4422));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4430));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                columns: new[] { "atualizado_em", "disponivel_para_ativar" },
                values: new object[] { new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4436), false });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(4443));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2426));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2576));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2807));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2951));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3287));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3418));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2356));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3531));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3567));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3606));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3639));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3674));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3718));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3753));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3787));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3821));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(3862));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(1405), "$2a$11$/m9CO2ah6gRTePvAgL4OneYfUDZK9lPl8qSwxA6./sBuRDo..d2n6" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 50, 59, 131, DateTimeKind.Utc).AddTicks(2178));
        }
    }
}
