using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDadosContratoChacara : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cliente_cep",
                table: "reservas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cliente_endereco",
                table: "reservas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "data_confirmacao",
                table: "reservas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cidade_assinatura",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locador_cpf",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locador_endereco",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locador_nome",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locador_rg",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locador_telefone",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4092));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4095));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4098));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4101));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4103));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3037));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3120));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3252));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3338));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3540));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3605));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(2997));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3671));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3694));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3717));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3740));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3763));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3785));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3807));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3829));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3855));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(3877));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(2441), "$2a$11$i9YDU2.lt3sPsgS0JXjuhODgAwM5.MPUzs1Kf7Key/Ps/OZrmIaCi" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 15, 1, 24, 851, DateTimeKind.Utc).AddTicks(2920));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cliente_cep",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "cliente_endereco",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "data_confirmacao",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "cidade_assinatura",
                table: "infos_chacara");

            migrationBuilder.DropColumn(
                name: "locador_cpf",
                table: "infos_chacara");

            migrationBuilder.DropColumn(
                name: "locador_endereco",
                table: "infos_chacara");

            migrationBuilder.DropColumn(
                name: "locador_nome",
                table: "infos_chacara");

            migrationBuilder.DropColumn(
                name: "locador_rg",
                table: "infos_chacara");

            migrationBuilder.DropColumn(
                name: "locador_telefone",
                table: "infos_chacara");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7385));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7389));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7393));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7395));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7400));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7402));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(3585));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(5740));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(5960));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6351));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6419));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6481));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(3529));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6544));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6566));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6589));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6612));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6638));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6660));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(6683));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7084));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7186));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(7210));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(2278), "$2a$11$EhbU2Fj8ro/vXvi2mWvJvufAsMwjAscVR8SOYsX2pBTzN1BzWlM/a" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 14, 25, 11, 943, DateTimeKind.Utc).AddTicks(3456));
        }
    }
}
