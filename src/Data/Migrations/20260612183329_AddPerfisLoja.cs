using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfisLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campos_extras_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opcoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_campos_extras_loja", x => x.id);
                    table.ForeignKey(
                        name: "f_k_campos_extras_loja__lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categorias_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_categorias_loja", x => x.id);
                    table.ForeignKey(
                        name: "f_k_categorias_loja__lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "perfis_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_perfis_loja", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campos_extras_perfil",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil_loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opcoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_campos_extras_perfil", x => x.id);
                    table.ForeignKey(
                        name: "f_k_campos_extras_perfil__perfis_loja_perfil_loja_id",
                        column: x => x.perfil_loja_id,
                        principalTable: "perfis_loja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categorias_perfil_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil_loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_categorias_perfil_loja", x => x.id);
                    table.ForeignKey(
                        name: "f_k_categorias_perfil_loja__perfis_loja_perfil_loja_id",
                        column: x => x.perfil_loja_id,
                        principalTable: "perfis_loja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "perfis_loja",
                columns: new[] { "id", "ativo", "criado_em", "descricao", "icone", "nome" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9680), "Para lojas de semi joias, bijuterias e maquiagem", "💍", "Semi Joias e Maquiagem" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9800), "Para lojas de roupas e moda", "👕", "Vestuário" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9984), "Para lojas de sapatos, tênis e sandálias", "👟", "Calçados" }
                });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(6907), "$2a$11$n9.xWT9e1GC0que6CMFJn.YDJ94E3jN5vZsNcxX1ZRQ.tdqN4N1pq" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9576));

            migrationBuilder.InsertData(
                table: "campos_extras_perfil",
                columns: new[] { "id", "chave", "label", "obrigatorio", "opcoes", "ordem", "perfil_loja_id", "tipo" },
                values: new object[,]
                {
                    { new Guid("13000000-0000-0000-0000-000000000001"), "tamanho", "Tamanho", true, "PP,P,M,G,GG,XG", 0, new Guid("10000000-0000-0000-0000-000000000002"), "lista" },
                    { new Guid("13000000-0000-0000-0000-000000000002"), "cor", "Cor", false, null, 1, new Guid("10000000-0000-0000-0000-000000000002"), "texto" },
                    { new Guid("15000000-0000-0000-0000-000000000001"), "numero", "Número", true, "33,34,35,36,37,38,39,40,41,42,43,44,45", 0, new Guid("10000000-0000-0000-0000-000000000003"), "lista" },
                    { new Guid("15000000-0000-0000-0000-000000000002"), "cor", "Cor", false, null, 1, new Guid("10000000-0000-0000-0000-000000000003"), "texto" }
                });

            migrationBuilder.InsertData(
                table: "categorias_perfil_loja",
                columns: new[] { "id", "nome", "ordem", "perfil_loja_id" },
                values: new object[,]
                {
                    { new Guid("11000000-0000-0000-0000-000000000001"), "Semi Joias", 0, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("11000000-0000-0000-0000-000000000002"), "Maquiagem", 1, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("11000000-0000-0000-0000-000000000003"), "Acessórios", 2, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("11000000-0000-0000-0000-000000000004"), "Outro", 3, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("12000000-0000-0000-0000-000000000001"), "Camiseta", 0, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000002"), "Calça Jeans", 1, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000003"), "Vestido", 2, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000004"), "Bermuda", 3, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000005"), "Blusa", 4, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000006"), "Casaco", 5, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("12000000-0000-0000-0000-000000000007"), "Outro", 6, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("14000000-0000-0000-0000-000000000001"), "Tênis", 0, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("14000000-0000-0000-0000-000000000002"), "Sandália", 1, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("14000000-0000-0000-0000-000000000003"), "Bota", 2, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("14000000-0000-0000-0000-000000000004"), "Sapato", 3, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("14000000-0000-0000-0000-000000000005"), "Chinelo", 4, new Guid("10000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "i_x_campos_extras_loja_loja_id",
                table: "campos_extras_loja",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_campos_extras_perfil_perfil_loja_id",
                table: "campos_extras_perfil",
                column: "perfil_loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_categorias_loja_loja_id",
                table: "categorias_loja",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_categorias_perfil_loja_perfil_loja_id",
                table: "categorias_perfil_loja",
                column: "perfil_loja_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campos_extras_loja");

            migrationBuilder.DropTable(
                name: "campos_extras_perfil");

            migrationBuilder.DropTable(
                name: "categorias_loja");

            migrationBuilder.DropTable(
                name: "categorias_perfil_loja");

            migrationBuilder.DropTable(
                name: "perfis_loja");

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 11, 21, 48, 58, 846, DateTimeKind.Utc).AddTicks(2828), "$2a$11$thMWsRXhyY0/5BEl3sxsQubav6Aq//pbe89AGfHSC8LrYAxa6Y0Ty" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 11, 21, 48, 58, 846, DateTimeKind.Utc).AddTicks(3508));
        }
    }
}
