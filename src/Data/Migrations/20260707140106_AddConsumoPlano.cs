using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumoPlano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumos_plano",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assinatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_servico = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    venda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_consumos_plano", x => x.id);
                    table.ForeignKey(
                        name: "f_k_consumos_plano_assinaturas_cliente_assinatura_id",
                        column: x => x.assinatura_id,
                        principalTable: "assinaturas_cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4608));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4765));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4893));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5047));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5115));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5187));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4557));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5304));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5335));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5364));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5406));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5444));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(5474));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(3876), "$2a$11$1uZrMLm8/R9JEIQvEWYHX.7.gCea4xKnz5Lz3jJqfpHIzTzgqJ8JS" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 14, 1, 5, 328, DateTimeKind.Utc).AddTicks(4463));

            migrationBuilder.CreateIndex(
                name: "i_x_consumos_plano_assinatura_id",
                table: "consumos_plano",
                column: "assinatura_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumos_plano");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3164));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3266));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3507));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3608));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3680));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3751));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3122));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3826));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3855));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3882));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3910));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3940));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(4001));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(2217), "$2a$11$U3fHaBfY5ezwv0QtXZ.FUuSD4CxD.gK3yalnKpU0juHcYOFWiuvWu" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 7, 13, 20, 52, 34, DateTimeKind.Utc).AddTicks(3015));
        }
    }
}
