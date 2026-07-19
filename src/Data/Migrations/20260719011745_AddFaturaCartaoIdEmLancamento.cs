using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturaCartaoIdEmLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "fatura_cartao_id",
                table: "lancamentos_financeiros",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4881));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4887));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4890));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4893));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4895));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(3892));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(3987));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4219));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4308));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4374));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4479));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(3850));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4547));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4570));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4599));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4630));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4653));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(4679));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(3243), "$2a$11$4mwwb706iC9CSQ.g0x8TbORqKubBiPfU3J0nhyXD8m62hrUYpS9Le" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 19, 1, 17, 44, 604, DateTimeKind.Utc).AddTicks(3769));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fatura_cartao_id",
                table: "lancamentos_financeiros");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(5117));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(5124));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(5127));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(5130));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(5132));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4171));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4268));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4394));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4567));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4635));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4700));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4132));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4766));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4791));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4816));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4840));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4870));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4934));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(3544), "$2a$11$QAJcjiagswqsKOaqVri4xew/R6A.c0uO7Ya/MBnJAJ6ydUF2ogc9u" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 58, 59, 27, DateTimeKind.Utc).AddTicks(4044));
        }
    }
}
