using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoVariacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "produto_variacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tamanho = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outro_campo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estoque = table.Column<int>(type: "integer", nullable: false),
                    estoque_minimo = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_produto_variacoes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_produto_variacoes_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 20, 22, 25, 497, DateTimeKind.Utc).AddTicks(4268));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 20, 22, 25, 497, DateTimeKind.Utc).AddTicks(4360));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 20, 22, 25, 497, DateTimeKind.Utc).AddTicks(4474));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 12, 20, 22, 25, 497, DateTimeKind.Utc).AddTicks(3597), "$2a$11$3VqGyS3zP061uJpWwSr.WOFM/eiGTjx.spOar/zRFsZ2Z2OdfKsY6" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 20, 22, 25, 497, DateTimeKind.Utc).AddTicks(4215));

            migrationBuilder.CreateIndex(
                name: "i_x_produto_variacoes_produto_id",
                table: "produto_variacoes",
                column: "produto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "produto_variacoes");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9680));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9800));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 12, 18, 33, 28, 470, DateTimeKind.Utc).AddTicks(9984));

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
        }
    }
}
