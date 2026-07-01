using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamentoAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pago",
                table: "agendamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "venda_id",
                table: "agendamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5793));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6008));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6142));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6239));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6308));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(6381));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5151), "$2a$11$h8Fk1J7ra51X7Q8GKDmcnOfIp0q/TZR/KpO6xrEjtlDiU3W.ASor." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 20, 4, 47, 662, DateTimeKind.Utc).AddTicks(5719));

            migrationBuilder.CreateIndex(
                name: "i_x_agendamentos_venda_id",
                table: "agendamentos",
                column: "venda_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_agendamentos__vendas_venda_id",
                table: "agendamentos",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_agendamentos__vendas_venda_id",
                table: "agendamentos");

            migrationBuilder.DropIndex(
                name: "i_x_agendamentos_venda_id",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "pago",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "venda_id",
                table: "agendamentos");

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3820));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3927));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4140));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4236));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4353));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(4422));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(2969), "$2a$11$P1NMz01Q2.XUJO76SzSiHO7PUykz6m8ozq2g3BStw6ro6hlUXhhDe" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 1, 18, 43, 20, 776, DateTimeKind.Utc).AddTicks(3752));
        }
    }
}
