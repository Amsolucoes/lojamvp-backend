using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedConvenienciaMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6323));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6434));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6578));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6674));

            migrationBuilder.InsertData(
                table: "perfis_loja",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "icone", "nome" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000005"), true, new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6745), "Para lojas de conveniência, mercadinhos e similares", "🏪", "Conveniência" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), true, new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6933), "Para lojas de materiais de construção e ferragens", "🧱", "Material de Construção" }
                });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(5456), "$2a$11$gurY8I6WX84w4Nk3kaCF3u4WNJH65SniO7tu3lxy.hHa/GLTOSSGq" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 29, 19, 54, 52, 241, DateTimeKind.Utc).AddTicks(6235));

            migrationBuilder.InsertData(
                table: "categorias_perfil_loja",
                columns: new[] { "id", "nome", "ordem", "perfil_loja_id", "tipo_tamanho" },
                values: new object[,]
                {
                    { new Guid("17000000-0000-0000-0000-000000000001"), "Bebidas", 0, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000002"), "Salgados", 1, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000003"), "Doces", 2, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000004"), "Cigarros", 3, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000005"), "Higiene", 4, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000006"), "Limpeza", 5, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("17000000-0000-0000-0000-000000000007"), "Mercearia", 6, new Guid("10000000-0000-0000-0000-000000000005"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000001"), "Cimento e Argamassa", 0, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000002"), "Tijolos e Blocos", 1, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000003"), "Tintas", 2, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000004"), "Hidráulica", 3, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000005"), "Elétrica", 4, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000006"), "Ferramentas", 5, new Guid("10000000-0000-0000-0000-000000000006"), "letra" },
                    { new Guid("18000000-0000-0000-0000-000000000007"), "Madeiras", 6, new Guid("10000000-0000-0000-0000-000000000006"), "letra" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("17000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("18000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(6675));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(6778));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(6906));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(7122));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(5911), "$2a$11$/l3kCMdBP4Kv4B.xsHBf1.9GiUKLxpdBzllEE7WSEaI34Sk1YIree" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(6592));
        }
    }
}
