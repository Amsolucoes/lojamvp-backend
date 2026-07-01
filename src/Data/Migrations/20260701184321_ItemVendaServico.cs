using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class ItemVendaServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_itens_venda__produtos_produto_id",
                table: "itens_venda");

            migrationBuilder.AlterColumn<Guid>(
                name: "produto_id",
                table: "itens_venda",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "servico_id",
                table: "itens_venda",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3820));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3927));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4140));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4236));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4353));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4422));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(2969), "$2a$11$P1NMz01Q2.XUJO76SzSiHO7PUykz6m8ozq2g3BStw6ro6hlUXhhDe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3752));

            migrationBuilder.CreateIndex(
                name: "i_x_itens_venda_servico_id",
                table: "itens_venda",
                column: "servico_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_itens_venda__produtos_produto_id",
                table: "itens_venda",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_itens_venda__servicos_servico_id",
                table: "itens_venda",
                column: "servico_id",
                principalTable: "servicos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_itens_venda__produtos_produto_id",
                table: "itens_venda");

            migrationBuilder.DropForeignKey(
                name: "f_k_itens_venda__servicos_servico_id",
                table: "itens_venda");

            migrationBuilder.DropIndex(
                name: "i_x_itens_venda_servico_id",
                table: "itens_venda");

            migrationBuilder.DropColumn(
                name: "servico_id",
                table: "itens_venda");

            migrationBuilder.AlterColumn<Guid>(
                name: "produto_id",
                table: "itens_venda",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(2966));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(3063));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(3201));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(3297));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(3362));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(3495));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(2083), "$2a$11$3qdA0VFuz9k0ndkoMurYc.x1cZ8ct6KOJqBc8Yy/RbYSVE9HuXJYG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 17, 52, 34, 158, DateTimeKind.Utc).AddTicks(2887));

            migrationBuilder.AddForeignKey(
                name: "f_k_itens_venda__produtos_produto_id",
                table: "itens_venda",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
