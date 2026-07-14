using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloCorretora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seguradoras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_seguradoras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "apolices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oportunidade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    seguradora_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_plano = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    numero_apolice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    valor_premio = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_comissao = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    percentual_comissao = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    vigencia_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    vigencia_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    renovada_para_apolice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lancamento_financeiro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_apolices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_apolices__seguradoras_seguradora_id",
                        column: x => x.seguradora_id,
                        principalTable: "seguradoras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "oportunidades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seguradora_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plano_desejado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    valor_estimado = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    etapa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    motivo_perda = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_oportunidades", x => x.id);
                    table.ForeignKey(
                        name: "f_k_oportunidades__seguradoras_seguradora_id",
                        column: x => x.seguradora_id,
                        principalTable: "seguradoras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3727));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3730));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3736));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3738));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3741));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(2599));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(2709));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(2918));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3126));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3230));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3304));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(2557));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3379));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3407));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3436));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3463));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3490));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(3517));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(1923), "$2a$11$8uNnOQ/Vv8e1wFZn7L6Rb.omgNBWviFrjCYFPS/JMyx1QPmykOify" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 21, 11, 13, 679, DateTimeKind.Utc).AddTicks(2467));

            migrationBuilder.CreateIndex(
                name: "i_x_apolices_seguradora_id",
                table: "apolices",
                column: "seguradora_id");

            migrationBuilder.CreateIndex(
                name: "i_x_oportunidades_seguradora_id",
                table: "oportunidades",
                column: "seguradora_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apolices");

            migrationBuilder.DropTable(
                name: "oportunidades");

            migrationBuilder.DropTable(
                name: "seguradoras");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2352));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2357));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2402));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2405));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2407));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1507));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1597));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1740));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1822));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1881));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1940));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1462));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2067));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2089));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2113));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2135));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2157));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(2184));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(634), "$2a$11$kb6Ubo7FjaH5YzpeEuHYXOz12urIAyoHkAis.35NsuTLIQCS9hv8y" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 14, 20, 41, 58, 558, DateTimeKind.Utc).AddTicks(1180));
        }
    }
}
