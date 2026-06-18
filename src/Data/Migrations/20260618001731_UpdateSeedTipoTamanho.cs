using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedTipoTamanho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000002"),
                column: "tipo_tamanho",
                value: "numero");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "nummero");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 17, 30, 750, DateTimeKind.Utc).AddTicks(7174));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 17, 30, 750, DateTimeKind.Utc).AddTicks(7267));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 17, 30, 750, DateTimeKind.Utc).AddTicks(7461));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 18, 0, 17, 30, 750, DateTimeKind.Utc).AddTicks(5630), "$2a$11$SwECatjYeW5PDI0M7VnQduVw1r.MjzgX.Hw6Tj/Mj3Q9nj/KP.yW6" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 17, 30, 750, DateTimeKind.Utc).AddTicks(7096));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000002"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 10, 27, 959, DateTimeKind.Utc).AddTicks(7131));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 10, 27, 959, DateTimeKind.Utc).AddTicks(7215));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 10, 27, 959, DateTimeKind.Utc).AddTicks(7347));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 18, 0, 10, 27, 959, DateTimeKind.Utc).AddTicks(6194), "$2a$11$PouaGa5EIruqsu0tjqAf0e2fTsMjUQLNOIXM0TSpY/42WOoipUyLS" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 6, 18, 0, 10, 27, 959, DateTimeKind.Utc).AddTicks(6984));
        }
    }
}
