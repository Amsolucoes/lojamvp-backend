using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaMotivoCancelamentoReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "motivo_cancelamento",
                table: "reservas",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7233));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7237));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7239));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7241));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7290));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7293));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7299));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7302));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7304));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7306));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7309));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(7312));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6103));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6298));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6434));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6524));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6589));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6659));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6065));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6764));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6789));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6815));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6840));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6865));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6889));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6914));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6963));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(6986));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(5378), "$2a$11$aPlLDxRBsvrYb3I60O4EQOJSaT/fR61dtN8Rl/0RJlbNtyV7Ohlea" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 21, 22, 30, 260, DateTimeKind.Utc).AddTicks(5985));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "motivo_cancelamento",
                table: "reservas");

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4191));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4195));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4197));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4202));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4247));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4250));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4254));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4256));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4258));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4260));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4263));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(4267));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3200));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3298));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3576));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3644));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(2927));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3714));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3740));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3767));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3795));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3820));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3844));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3869));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3894));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3919));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(3945));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(2395), "$2a$11$8tAgl.0goJh8DJ0v0zG2u.FdMp8d1b/SuDjxwzsLbaZRqU6EAdZjO" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 6, 20, 13, 45, 608, DateTimeKind.Utc).AddTicks(2856));
        }
    }
}
