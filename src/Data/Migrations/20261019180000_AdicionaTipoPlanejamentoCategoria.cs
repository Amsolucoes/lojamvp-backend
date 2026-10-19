using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261019180000_AdicionaTipoPlanejamentoCategoria")]
    public partial class AdicionaTipoPlanejamentoCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "planejamento_categorias",
                type: "text",
                nullable: false,
                defaultValue: "pagar");

            migrationBuilder.DropIndex(
                name: "i_x_planejamento_categorias_loja_id_categoria_id",
                table: "planejamento_categorias");

            migrationBuilder.CreateIndex(
                name: "i_x_planejamento_categorias_loja_id_categoria_id_tipo",
                table: "planejamento_categorias",
                columns: new[] { "loja_id", "categoria_id", "tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_planejamento_categorias_loja_id_categoria_id_tipo",
                table: "planejamento_categorias");

            migrationBuilder.DropColumn(name: "tipo", table: "planejamento_categorias");

            migrationBuilder.CreateIndex(
                name: "i_x_planejamento_categorias_loja_id_categoria_id",
                table: "planejamento_categorias",
                columns: new[] { "loja_id", "categoria_id" },
                unique: true);
        }
    }
}
