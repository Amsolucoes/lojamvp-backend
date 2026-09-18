using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaLancamentoManualNf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_nfs_importadas_chave_acesso",
                table: "nfs_importadas");

            migrationBuilder.AlterColumn<string>(
                name: "chave_acesso",
                table: "nfs_importadas",
                type: "character varying(44)",
                maxLength: 44,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(44)",
                oldMaxLength: 44);

            migrationBuilder.AddColumn<DateTime>(
                name: "data_emissao",
                table: "nfs_importadas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fornecedor_id",
                table: "nfs_importadas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origem",
                table: "nfs_importadas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "xml");

            migrationBuilder.AddColumn<decimal>(
                name: "valor_total",
                table: "nfs_importadas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8895));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8902));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8908));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8913));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8976));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8985));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8995));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9001));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9007));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9012));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9017));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9023));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(9028));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7441));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7572));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7772));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7903));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8039));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8159));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7384));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8264));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8298));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8333));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8366));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8402));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8444));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8475));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8507));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8537));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(8568));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(6801), "$2a$11$66pYBDWQPBiaMS5YpDhSFeMkJeGiRGEccEyDLgSqIK2BBlS059I5e" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 18, 14, 12, 40, 149, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.CreateIndex(
                name: "i_x_nfs_importadas_fornecedor_id",
                table: "nfs_importadas",
                column: "fornecedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_nfs_importadas_chave_acesso",
                table: "nfs_importadas",
                columns: new[] { "loja_id", "chave_acesso" },
                unique: true,
                filter: "chave_acesso IS NOT NULL AND chave_acesso <> '' AND NOT desfeita");

            migrationBuilder.AddForeignKey(
                name: "f_k_nfs_importadas_fornecedores_fornecedor_id",
                table: "nfs_importadas",
                column: "fornecedor_id",
                principalTable: "fornecedores",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_nfs_importadas_fornecedores_fornecedor_id",
                table: "nfs_importadas");

            migrationBuilder.DropIndex(
                name: "i_x_nfs_importadas_fornecedor_id",
                table: "nfs_importadas");

            migrationBuilder.DropIndex(
                name: "ix_nfs_importadas_chave_acesso",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "data_emissao",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "fornecedor_id",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "origem",
                table: "nfs_importadas");

            migrationBuilder.DropColumn(
                name: "valor_total",
                table: "nfs_importadas");

            migrationBuilder.AlterColumn<string>(
                name: "chave_acesso",
                table: "nfs_importadas",
                type: "character varying(44)",
                maxLength: 44,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(44)",
                oldMaxLength: 44,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3113));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3124));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3129));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3134));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3225));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3236));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3246));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3251));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3257));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3266));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3272));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3279));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(3287));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(84));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(224));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(452));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(589));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1572));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1789));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(17));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1896));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1928));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1966));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(1998));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2026));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2067));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2097));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2126));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2157));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 670, DateTimeKind.Utc).AddTicks(2187));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 17, 19, 12, 31, 669, DateTimeKind.Utc).AddTicks(9170), "$2a$11$VPiD7POxTiqu5ZfWE6uNDO5URBC04909dkEkyvnKoAjQ0FknF33l2" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 17, 19, 12, 31, 669, DateTimeKind.Utc).AddTicks(9861));

            migrationBuilder.CreateIndex(
                name: "ix_nfs_importadas_chave_acesso",
                table: "nfs_importadas",
                columns: new[] { "loja_id", "chave_acesso" },
                unique: true,
                filter: "NOT desfeita");
        }
    }
}
