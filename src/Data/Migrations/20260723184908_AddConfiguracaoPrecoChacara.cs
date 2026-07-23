using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracaoPrecoChacara : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracoes_preco_chacara",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_diaria_semana = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_diaria_fim_semana = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_diaria_fim_semana_grande = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_pacote2_dias_fim_semana = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_pacote2_dias_fim_semana_grande = table.Column<decimal>(type: "numeric", nullable: false),
                    limite_pessoas_pacote_pequeno = table.Column<int>(type: "integer", nullable: false),
                    valor_taxa_limpeza = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_multa_nao_limpeza = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_configuracoes_preco_chacara", x => x.id);
                    table.ForeignKey(
                        name: "f_k_configuracoes_preco_chacara_lojas_loja_id",
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
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3314));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3318));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3323));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3326));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3331));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3334));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2049));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2164));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2320));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2431));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2661));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2742));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2002));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2824));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2856));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2888));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2920));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2950));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(2979));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3009));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3040));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(1357), "$2a$11$qa9GvEy/tAXvx36Sc9k5HO/EBWvNgX.NFxVYslAWVU05e0qk0H5Fy" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 49, 6, 691, DateTimeKind.Utc).AddTicks(1900));

            migrationBuilder.CreateIndex(
                name: "i_x_configuracoes_preco_chacara_loja_id",
                table: "configuracoes_preco_chacara",
                column: "loja_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_preco_chacara");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4082));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4086));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4092));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4094));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4096));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(4099));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3033));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3121));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3261));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3455));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3536));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3609));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(2995));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3675));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3701));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3727));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3751));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3774));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3798));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3821));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3844));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(3868));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(2318), "$2a$11$ixxWQLvDXTOhwiDmdczfH.qaufkGEr9gqf1Y4U7Lq1Ck/9E5UAowe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 23, 18, 9, 57, 459, DateTimeKind.Utc).AddTicks(2892));
        }
    }
}
