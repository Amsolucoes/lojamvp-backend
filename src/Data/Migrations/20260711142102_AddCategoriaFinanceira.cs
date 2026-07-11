using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaFinanceira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_financeiros_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_financeiros");

            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_fixos_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_fixos");

            migrationBuilder.DropColumn(
                name: "categoria",
                table: "lancamentos_fixos");

            migrationBuilder.DropColumn(
                name: "categoria",
                table: "lancamentos_financeiros");

            migrationBuilder.AddColumn<Guid>(
                name: "categoria_id",
                table: "lancamentos_fixos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "categoria_id",
                table: "lancamentos_financeiros",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias_financeiras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    icone = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_categorias_financeiras", x => x.id);
                    table.ForeignKey(
                        name: "f_k_categorias_financeiras__lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8603));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8704));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8841));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9015));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9086));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9302));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8560));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9437));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9470));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9500));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9529));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9557));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(9584));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(7826), "$2a$11$t6vOay27OrqORDaHYGcOguQ/o3ofijHVRxijNUYpwWoBVM7K8AAfW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 21, 1, 945, DateTimeKind.Utc).AddTicks(8469));

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_fixos_categoria_id",
                table: "lancamentos_fixos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_lancamentos_financeiros_categoria_id",
                table: "lancamentos_financeiros",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_categorias_financeiras_loja_id",
                table: "categorias_financeiras",
                column: "loja_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_financeiros_categorias_financeiras_categoria_id",
                table: "lancamentos_financeiros",
                column: "categoria_id",
                principalTable: "categorias_financeiras",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_financeiros_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_financeiros",
                column: "conta_bancaria_id",
                principalTable: "contas_bancarias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_fixos_categorias_financeiras_categoria_id",
                table: "lancamentos_fixos",
                column: "categoria_id",
                principalTable: "categorias_financeiras",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_fixos_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_fixos",
                column: "conta_bancaria_id",
                principalTable: "contas_bancarias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_financeiros_categorias_financeiras_categoria_id",
                table: "lancamentos_financeiros");

            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_financeiros_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_financeiros");

            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_fixos_categorias_financeiras_categoria_id",
                table: "lancamentos_fixos");

            migrationBuilder.DropForeignKey(
                name: "f_k_lancamentos_fixos_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_fixos");

            migrationBuilder.DropTable(
                name: "categorias_financeiras");

            migrationBuilder.DropIndex(
                name: "i_x_lancamentos_fixos_categoria_id",
                table: "lancamentos_fixos");

            migrationBuilder.DropIndex(
                name: "i_x_lancamentos_financeiros_categoria_id",
                table: "lancamentos_financeiros");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "lancamentos_fixos");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "lancamentos_financeiros");

            migrationBuilder.AddColumn<string>(
                name: "categoria",
                table: "lancamentos_fixos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "categoria",
                table: "lancamentos_financeiros",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7977));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8084));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8223));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8438));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8517));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8594));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7932));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8674));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8705));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8736));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8766));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8829));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(8859));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7069), "$2a$11$bVH7mLGB.MZbXEzEST3wRufffk6lV.XS.zmBEkUoOKPrJmle5aviy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 11, 14, 5, 16, 340, DateTimeKind.Utc).AddTicks(7837));

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_financeiros_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_financeiros",
                column: "conta_bancaria_id",
                principalTable: "contas_bancarias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_lancamentos_fixos_contas_bancarias_conta_bancaria_id",
                table: "lancamentos_fixos",
                column: "conta_bancaria_id",
                principalTable: "contas_bancarias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
