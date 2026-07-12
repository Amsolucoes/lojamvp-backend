using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParceladoFixoCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cartao_fixo_id",
                table: "lancamentos_cartao",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "grupo_parcelamento_id",
                table: "lancamentos_cartao",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modo",
                table: "lancamentos_cartao",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "numero_parcela",
                table: "lancamentos_cartao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_parcelas",
                table: "lancamentos_cartao",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cartao_lancamentos_fixos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_credito_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    gerado_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_cartao_lancamentos_fixos", x => x.id);
                    table.ForeignKey(
                        name: "f_k_cartao_lancamentos_fixos__categorias_financeiras_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_financeiras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_cartao_lancamentos_fixos_cartoes_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartoes_credito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9267));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9402));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9537));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9624));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9694));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9759));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9225));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9833));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9856));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9881));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9905));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9930));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9952));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(8477), "$2a$11$YCyGmZ9c1HWhV02u8iMRL.v9LqKIryrczHMMh1iCl6L29o0tqg/C2" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 11, 23, 15, 759, DateTimeKind.Utc).AddTicks(9137));

            migrationBuilder.CreateIndex(
                name: "i_x_cartao_lancamentos_fixos_cartao_credito_id",
                table: "cartao_lancamentos_fixos",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "i_x_cartao_lancamentos_fixos_categoria_id",
                table: "cartao_lancamentos_fixos",
                column: "categoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cartao_lancamentos_fixos");

            migrationBuilder.DropColumn(
                name: "cartao_fixo_id",
                table: "lancamentos_cartao");

            migrationBuilder.DropColumn(
                name: "grupo_parcelamento_id",
                table: "lancamentos_cartao");

            migrationBuilder.DropColumn(
                name: "modo",
                table: "lancamentos_cartao");

            migrationBuilder.DropColumn(
                name: "numero_parcela",
                table: "lancamentos_cartao");

            migrationBuilder.DropColumn(
                name: "total_parcelas",
                table: "lancamentos_cartao");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(4978));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5090));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5232));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5339));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5419));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5570));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(4935));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5649));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5679));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5710));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5740));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5769));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(5802));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(4315), "$2a$11$yCYG3qAQi6.VI7aYEUwIS.N0I7X4M7rrTySFPIjcBJC6HQhd4DDMy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 12, 1, 4, 33, 422, DateTimeKind.Utc).AddTicks(4847));
        }
    }
}
