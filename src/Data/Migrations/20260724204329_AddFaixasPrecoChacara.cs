using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaixasPrecoChacara : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valor_diaria_fim_semana",
                table: "configuracoes_preco_chacara");

            migrationBuilder.DropColumn(
                name: "valor_diaria_fim_semana_grande",
                table: "configuracoes_preco_chacara");

            migrationBuilder.DropColumn(
                name: "valor_diaria_semana",
                table: "configuracoes_preco_chacara");

            migrationBuilder.DropColumn(
                name: "valor_pacote2_dias_fim_semana",
                table: "configuracoes_preco_chacara");

            migrationBuilder.DropColumn(
                name: "valor_pacote2_dias_fim_semana_grande",
                table: "configuracoes_preco_chacara");

            migrationBuilder.RenameColumn(
                name: "limite_pessoas_pacote_pequeno",
                table: "configuracoes_preco_chacara",
                newName: "limite_pessoas_para_taxa_limpeza");

            migrationBuilder.CreateTable(
                name: "faixas_preco_chacara",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pessoas_ate = table.Column<int>(type: "integer", nullable: false),
                    valor_diaria_semana = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_diaria_fim_semana = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_pacote2_dias_fim_semana = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_faixas_preco_chacara", x => x.id);
                    table.ForeignKey(
                        name: "f_k_faixas_preco_chacara_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7073));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7076));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7080));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7083));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7087));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(7090));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(5977));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6077));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6318));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6406));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6475));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6539));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(5941));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6603));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6660));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6685));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6709));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6736));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6760));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6783));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6807));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6831));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(6854));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(5334), "$2a$11$ccLp3pXidKdO8OC80QukHOhTnOPLjQ4f1kzqEliBvMokvUcqnX9rG" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 24, 20, 43, 28, 911, DateTimeKind.Utc).AddTicks(5863));

            migrationBuilder.CreateIndex(
                name: "i_x_faixas_preco_chacara_loja_id_pessoas_ate",
                table: "faixas_preco_chacara",
                columns: new[] { "loja_id", "pessoas_ate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faixas_preco_chacara");

            migrationBuilder.RenameColumn(
                name: "limite_pessoas_para_taxa_limpeza",
                table: "configuracoes_preco_chacara",
                newName: "limite_pessoas_pacote_pequeno");

            migrationBuilder.AddColumn<decimal>(
                name: "valor_diaria_fim_semana",
                table: "configuracoes_preco_chacara",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_diaria_fim_semana_grande",
                table: "configuracoes_preco_chacara",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_diaria_semana",
                table: "configuracoes_preco_chacara",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_pacote2_dias_fim_semana",
                table: "configuracoes_preco_chacara",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_pacote2_dias_fim_semana_grande",
                table: "configuracoes_preco_chacara",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

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
    }
}
