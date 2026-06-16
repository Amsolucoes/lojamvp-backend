using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormasPagamentoVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "formas_pagamento",
                table: "vendas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 16, 19, 59, 26, 120, DateTimeKind.Utc).AddTicks(2293));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 16, 19, 59, 26, 120, DateTimeKind.Utc).AddTicks(2398));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 16, 19, 59, 26, 120, DateTimeKind.Utc).AddTicks(2525));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 16, 19, 59, 26, 120, DateTimeKind.Utc).AddTicks(1346), "$2a$11$WB5R84Hl0jiR/AUMOReZ9.oCTiGrRvdpB5SqreRa.G/Yccm9sCkwy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 16, 19, 59, 26, 120, DateTimeKind.Utc).AddTicks(2118));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "formas_pagamento",
                table: "vendas");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 13, 19, 6, 18, 660, DateTimeKind.Utc).AddTicks(4785));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 13, 19, 6, 18, 660, DateTimeKind.Utc).AddTicks(4889));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 13, 19, 6, 18, 660, DateTimeKind.Utc).AddTicks(5009));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 13, 19, 6, 18, 660, DateTimeKind.Utc).AddTicks(3952), "$2a$11$eah45AAEqXmqCmTEUL7clugxO4d7Nv9u5byMUM3uFb96mkEm5/FkG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 13, 19, 6, 18, 660, DateTimeKind.Utc).AddTicks(4611));
        }
    }
}
