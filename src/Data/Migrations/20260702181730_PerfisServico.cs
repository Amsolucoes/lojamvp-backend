using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerfisServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo_plano_aplica",
                table: "perfis_loja",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "servicos_perfil_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil_loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    duracao_min = table.Column<int>(type: "integer", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_servicos_perfil_loja", x => x.id);
                    table.ForeignKey(
                        name: "f_k_servicos_perfil_loja_perfis_loja_perfil_loja_id",
                        column: x => x.perfil_loja_id,
                        principalTable: "perfis_loja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(7874), "loja" });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(7963), "loja" });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8098), "loja" });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8192), "loja" });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8262), "loja" });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "criado_em", "tipo_plano_aplica" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8330), "loja" });

            migrationBuilder.InsertData(
                table: "perfis_loja",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "icone", "nome", "tipo_plano_aplica" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000009"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(7679), "Loja em branco — você cria suas próprias categorias", "🏬", "Começar do zero", "loja" },
                    { new Guid("20000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8429), "Para banho e tosa: agenda, serviços e caixa", "🐾", "Banho e Tosa", "servicos" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8454), "Banho e tosa com venda de produtos", "🐾", "Banho e Tosa + Loja", "loja_modulos" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8481), "Para barbearias: agenda, serviços e caixa", "💈", "Barbearia", "servicos" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8506), "Barbearia com venda de produtos", "💈", "Barbearia + Loja", "loja_modulos" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8532), "Para salões: agenda, serviços e caixa", "💇", "Salão de Beleza", "servicos" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), true, new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(8556), "Salão com venda de produtos", "💇", "Salão de Beleza + Loja", "loja_modulos" }
                });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(7054), "$2a$11$QtrQ2e5qsVY1beO6evWsge0DBqvYYOtxjgXZj6eZkSr3o6j4U1MBC" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 2, 18, 17, 29, 368, DateTimeKind.Utc).AddTicks(7597));

            migrationBuilder.InsertData(
                table: "servicos_perfil_loja",
                columns: new[] { "id", "categoria", "duracao_min", "nome", "ordem", "perfil_loja_id", "preco" },
                values: new object[,]
                {
                    { new Guid("21a00000-0000-0000-0000-000000000001"), "Banho", 60, "Banho (porte pequeno)", 0, new Guid("20000000-0000-0000-0000-000000000001"), 40m },
                    { new Guid("21a00000-0000-0000-0000-000000000002"), "Banho", 75, "Banho (porte médio)", 1, new Guid("20000000-0000-0000-0000-000000000001"), 55m },
                    { new Guid("21a00000-0000-0000-0000-000000000003"), "Banho", 90, "Banho (porte grande)", 2, new Guid("20000000-0000-0000-0000-000000000001"), 75m },
                    { new Guid("21a00000-0000-0000-0000-000000000004"), "Tosa", 45, "Tosa higiênica", 3, new Guid("20000000-0000-0000-0000-000000000001"), 35m },
                    { new Guid("21a00000-0000-0000-0000-000000000005"), "Tosa", 90, "Tosa completa", 4, new Guid("20000000-0000-0000-0000-000000000001"), 70m },
                    { new Guid("21b00000-0000-0000-0000-000000000001"), "Banho", 60, "Banho (porte pequeno)", 0, new Guid("20000000-0000-0000-0000-000000000002"), 40m },
                    { new Guid("21b00000-0000-0000-0000-000000000002"), "Banho", 75, "Banho (porte médio)", 1, new Guid("20000000-0000-0000-0000-000000000002"), 55m },
                    { new Guid("21b00000-0000-0000-0000-000000000003"), "Banho", 90, "Banho (porte grande)", 2, new Guid("20000000-0000-0000-0000-000000000002"), 75m },
                    { new Guid("21b00000-0000-0000-0000-000000000004"), "Tosa", 45, "Tosa higiênica", 3, new Guid("20000000-0000-0000-0000-000000000002"), 35m },
                    { new Guid("21b00000-0000-0000-0000-000000000005"), "Tosa", 90, "Tosa completa", 4, new Guid("20000000-0000-0000-0000-000000000002"), 70m },
                    { new Guid("21c00000-0000-0000-0000-000000000001"), "Cabelo", 30, "Corte de cabelo", 0, new Guid("20000000-0000-0000-0000-000000000003"), 35m },
                    { new Guid("21c00000-0000-0000-0000-000000000002"), "Barba", 30, "Barba", 1, new Guid("20000000-0000-0000-0000-000000000003"), 25m },
                    { new Guid("21c00000-0000-0000-0000-000000000003"), "Combo", 60, "Corte + Barba", 2, new Guid("20000000-0000-0000-0000-000000000003"), 55m },
                    { new Guid("21c00000-0000-0000-0000-000000000004"), "Acabamento", 15, "Pezinho / acabamento", 3, new Guid("20000000-0000-0000-0000-000000000003"), 15m },
                    { new Guid("21c00000-0000-0000-0000-000000000005"), "Sobrancelha", 15, "Sobrancelha masculina", 4, new Guid("20000000-0000-0000-0000-000000000003"), 15m },
                    { new Guid("21d00000-0000-0000-0000-000000000001"), "Cabelo", 30, "Corte de cabelo", 0, new Guid("20000000-0000-0000-0000-000000000004"), 35m },
                    { new Guid("21d00000-0000-0000-0000-000000000002"), "Barba", 30, "Barba", 1, new Guid("20000000-0000-0000-0000-000000000004"), 25m },
                    { new Guid("21d00000-0000-0000-0000-000000000003"), "Combo", 60, "Corte + Barba", 2, new Guid("20000000-0000-0000-0000-000000000004"), 55m },
                    { new Guid("21d00000-0000-0000-0000-000000000004"), "Acabamento", 15, "Pezinho / acabamento", 3, new Guid("20000000-0000-0000-0000-000000000004"), 15m },
                    { new Guid("21d00000-0000-0000-0000-000000000005"), "Sobrancelha", 15, "Sobrancelha masculina", 4, new Guid("20000000-0000-0000-0000-000000000004"), 15m },
                    { new Guid("21e00000-0000-0000-0000-000000000001"), "Cabelo", 60, "Corte feminino", 0, new Guid("20000000-0000-0000-0000-000000000005"), 60m },
                    { new Guid("21e00000-0000-0000-0000-000000000002"), "Cabelo", 45, "Escova", 1, new Guid("20000000-0000-0000-0000-000000000005"), 45m },
                    { new Guid("21e00000-0000-0000-0000-000000000003"), "Química", 120, "Coloração / tintura", 2, new Guid("20000000-0000-0000-0000-000000000005"), 120m },
                    { new Guid("21e00000-0000-0000-0000-000000000004"), "Unhas", 45, "Manicure", 3, new Guid("20000000-0000-0000-0000-000000000005"), 35m },
                    { new Guid("21e00000-0000-0000-0000-000000000005"), "Unhas", 45, "Pedicure", 4, new Guid("20000000-0000-0000-0000-000000000005"), 40m },
                    { new Guid("21f00000-0000-0000-0000-000000000001"), "Cabelo", 60, "Corte feminino", 0, new Guid("20000000-0000-0000-0000-000000000006"), 60m },
                    { new Guid("21f00000-0000-0000-0000-000000000002"), "Cabelo", 45, "Escova", 1, new Guid("20000000-0000-0000-0000-000000000006"), 45m },
                    { new Guid("21f00000-0000-0000-0000-000000000003"), "Química", 120, "Coloração / tintura", 2, new Guid("20000000-0000-0000-0000-000000000006"), 120m },
                    { new Guid("21f00000-0000-0000-0000-000000000004"), "Unhas", 45, "Manicure", 3, new Guid("20000000-0000-0000-0000-000000000006"), 35m },
                    { new Guid("21f00000-0000-0000-0000-000000000005"), "Unhas", 45, "Pedicure", 4, new Guid("20000000-0000-0000-0000-000000000006"), 40m }
                });

            migrationBuilder.CreateIndex(
                name: "i_x_servicos_perfil_loja_perfil_loja_id",
                table: "servicos_perfil_loja",
                column: "perfil_loja_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servicos_perfil_loja");

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "tipo_plano_aplica",
                table: "perfis_loja");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5793));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6008));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6142));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6239));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6308));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6381));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5151), "$2a$11$h8Fk1J7ra51X7Q8GKDmcnOfIp0q/TZR/KpO6xrEjtlDiU3W.ASor." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5719));
        }
    }
}
