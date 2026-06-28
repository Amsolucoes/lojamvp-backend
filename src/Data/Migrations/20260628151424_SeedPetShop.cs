using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPetShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "perfis_loja",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "icone", "nome" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000004"), true, new DateTime(2026, 6, 28, 15, 14, 23, 866, DateTimeKind.Utc).AddTicks(7122), "Para pet shops: ração a granel, petiscos, acessórios e mais", "🐾", "Pet Shop" });

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

            migrationBuilder.InsertData(
                table: "categorias_perfil_loja",
                columns: new[] { "id", "nome", "ordem", "perfil_loja_id", "tipo_tamanho" },
                values: new object[,]
                {
                    { new Guid("16000000-0000-0000-0000-000000000001"), "Ração", 0, new Guid("10000000-0000-0000-0000-000000000004"), "letra" },
                    { new Guid("16000000-0000-0000-0000-000000000002"), "Petiscos", 1, new Guid("10000000-0000-0000-0000-000000000004"), "letra" },
                    { new Guid("16000000-0000-0000-0000-000000000003"), "Brinquedos", 2, new Guid("10000000-0000-0000-0000-000000000004"), "letra" },
                    { new Guid("16000000-0000-0000-0000-000000000004"), "Higiene", 3, new Guid("10000000-0000-0000-0000-000000000004"), "letra" },
                    { new Guid("16000000-0000-0000-0000-000000000005"), "Acessórios", 4, new Guid("10000000-0000-0000-0000-000000000004"), "letra" },
                    { new Guid("16000000-0000-0000-0000-000000000006"), "Medicamentos", 5, new Guid("10000000-0000-0000-0000-000000000004"), "letra" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("16000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

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
    }
}
