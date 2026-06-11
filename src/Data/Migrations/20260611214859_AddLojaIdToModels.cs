using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLojaIdToModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "loja_id",
                table: "vendas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "loja_id",
                table: "produtos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "loja_id",
                table: "movimentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "loja_id",
                table: "clientes",
                type: "uuid",
                nullable: true);

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
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 11, 21, 48, 58, 846, DateTimeKind.Utc).AddTicks(3508), "$2a$11$pJv1QqzqQHz4rG17gLCUoORPXG8/9fS3mtJpTuULzEYV/qc7heetu" });

            migrationBuilder.CreateIndex(
                name: "i_x_vendas_loja_id",
                table: "vendas",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_produtos_loja_id",
                table: "produtos",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_movimentos_loja_id",
                table: "movimentos",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_clientes_loja_id",
                table: "clientes",
                column: "loja_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_clientes__lojas_loja_id",
                table: "clientes",
                column: "loja_id",
                principalTable: "lojas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_movimentos_lojas_loja_id",
                table: "movimentos",
                column: "loja_id",
                principalTable: "lojas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_produtos_lojas_loja_id",
                table: "produtos",
                column: "loja_id",
                principalTable: "lojas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_vendas_lojas_loja_id",
                table: "vendas",
                column: "loja_id",
                principalTable: "lojas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_clientes__lojas_loja_id",
                table: "clientes");

            migrationBuilder.DropForeignKey(
                name: "f_k_movimentos_lojas_loja_id",
                table: "movimentos");

            migrationBuilder.DropForeignKey(
                name: "f_k_produtos_lojas_loja_id",
                table: "produtos");

            migrationBuilder.DropForeignKey(
                name: "f_k_vendas_lojas_loja_id",
                table: "vendas");

            migrationBuilder.DropIndex(
                name: "i_x_vendas_loja_id",
                table: "vendas");

            migrationBuilder.DropIndex(
                name: "i_x_produtos_loja_id",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "i_x_movimentos_loja_id",
                table: "movimentos");

            migrationBuilder.DropIndex(
                name: "i_x_clientes_loja_id",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "loja_id",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "loja_id",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "loja_id",
                table: "movimentos");

            migrationBuilder.DropColumn(
                name: "loja_id",
                table: "clientes");

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 10, 21, 30, 40, 465, DateTimeKind.Utc).AddTicks(196), "$2a$11$x8yJgA2M3MU6G6DUVUexyOlhYkqGauf71fjMatQk3npxkzZrM6Om." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 10, 21, 30, 40, 612, DateTimeKind.Utc).AddTicks(6829), "$2a$11$CGaeLrB0/ky/BSKg72FJ1uNQ3CI6lRWs84SqAhlYORC5zm/rot5iW" });
        }
    }
}
