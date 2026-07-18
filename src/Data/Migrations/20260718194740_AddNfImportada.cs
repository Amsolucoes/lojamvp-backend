using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNfImportada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nfs_importadas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave_acesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    numero_nf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome_fornecedor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    qtd_itens = table.Column<int>(type: "integer", nullable: false),
                    importado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_nfs_importadas", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1280));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1284));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1288));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1290));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1292));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(337));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(422));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(649));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(737));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(800));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(864));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(287));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(928));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(991));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1020));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1059));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1084));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(1107));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 18, 19, 47, 39, 353, DateTimeKind.Utc).AddTicks(9708), "$2a$11$6KKypp6fkXPFScqp0neCe.5YoH5vKIOreHxRSOew4Q6iKhLV2Ce4e" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 19, 47, 39, 354, DateTimeKind.Utc).AddTicks(199));

            migrationBuilder.CreateIndex(
                name: "i_x_nfs_importadas_loja_id_chave_acesso",
                table: "nfs_importadas",
                columns: new[] { "loja_id", "chave_acesso" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nfs_importadas");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9674));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9678));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9683));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9685));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9689));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8617));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8712));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8867));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8972));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9144));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9228));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8574));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9306));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9335));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9364));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9392));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9421));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(9449));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(7962), "$2a$11$PFdXUu3YVb9fOXNTHcOwn.45EmqXjlQrbqyUdws/Pb.MM0Yy00ZV." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 18, 15, 30, 6, 842, DateTimeKind.Utc).AddTicks(8480));
        }
    }
}
