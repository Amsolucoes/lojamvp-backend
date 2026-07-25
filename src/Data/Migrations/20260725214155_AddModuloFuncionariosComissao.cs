using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloFuncionariosComissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "comissao_padrao_percentual",
                table: "profissionais",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "profissional_id",
                table: "agendamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "comissoes_funcionario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_servico = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    comissao_percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    valor_comissao = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fechamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_comissoes_funcionario", x => x.id);
                    table.ForeignKey(
                        name: "f_k_comissoes_funcionario__profissionais_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "profissionais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_comissoes_funcionario_agendamentos_agendamento_id",
                        column: x => x.agendamento_id,
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comissoes_servico_profissional",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comissao_percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_comissoes_servico_profissional", x => x.id);
                    table.ForeignKey(
                        name: "f_k_comissoes_servico_profissional__profissionais_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "profissionais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fechamentos_comissao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    periodo_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    qtd_atendimentos = table.Column<int>(type: "integer", nullable: false),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lancamento_financeiro_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fechamentos_comissao", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fechamentos_comissao__profissionais_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "profissionais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2271));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2276));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2283));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2286));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2289));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2292));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1339));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1450));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1530));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1608));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(887));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1745));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1811));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1842));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1872));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1903));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1934));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1964));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(1993));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(2022));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 25, 21, 41, 54, 424, DateTimeKind.Utc).AddTicks(9890), "$2a$11$P9GsP9A6X6L/7assdKXUHeD8ahr2ikmV4b70T4N67Wa1Utp06apYe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 25, 21, 41, 54, 425, DateTimeKind.Utc).AddTicks(755));

            migrationBuilder.CreateIndex(
                name: "i_x_agendamentos_profissional_id",
                table: "agendamentos",
                column: "profissional_id");

            migrationBuilder.CreateIndex(
                name: "i_x_comissoes_funcionario_agendamento_id",
                table: "comissoes_funcionario",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "i_x_comissoes_funcionario_profissional_id",
                table: "comissoes_funcionario",
                column: "profissional_id");

            migrationBuilder.CreateIndex(
                name: "i_x_comissoes_servico_profissional_profissional_id_servico_id",
                table: "comissoes_servico_profissional",
                columns: new[] { "profissional_id", "servico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_fechamentos_comissao_profissional_id",
                table: "fechamentos_comissao",
                column: "profissional_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_agendamentos__profissionais_profissional_id",
                table: "agendamentos",
                column: "profissional_id",
                principalTable: "profissionais",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_agendamentos__profissionais_profissional_id",
                table: "agendamentos");

            migrationBuilder.DropTable(
                name: "comissoes_funcionario");

            migrationBuilder.DropTable(
                name: "comissoes_servico_profissional");

            migrationBuilder.DropTable(
                name: "fechamentos_comissao");

            migrationBuilder.DropIndex(
                name: "i_x_agendamentos_profissional_id",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "comissao_padrao_percentual",
                table: "profissionais");

            migrationBuilder.DropColumn(
                name: "profissional_id",
                table: "agendamentos");

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
        }
    }
}
