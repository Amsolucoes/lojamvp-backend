using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoTamanhoCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo_tamanho",
                table: "categorias_perfil_loja",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tipo_tamanho",
                table: "categorias_loja",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("11000000-0000-0000-0000-000000000001"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("11000000-0000-0000-0000-000000000002"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("11000000-0000-0000-0000-000000000003"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("11000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000001"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000002"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000003"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000005"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000006"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("12000000-0000-0000-0000-000000000007"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("14000000-0000-0000-0000-000000000001"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("14000000-0000-0000-0000-000000000002"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("14000000-0000-0000-0000-000000000003"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("14000000-0000-0000-0000-000000000004"),
                column: "tipo_tamanho",
                value: "letra");

            migrationBuilder.UpdateData(
                table: "categorias_perfil_loja",
                keyColumn: "id",
                keyValue: new Guid("14000000-0000-0000-0000-000000000005"),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo_tamanho",
                table: "categorias_perfil_loja");

            migrationBuilder.DropColumn(
                name: "tipo_tamanho",
                table: "categorias_loja");

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
    }
}
