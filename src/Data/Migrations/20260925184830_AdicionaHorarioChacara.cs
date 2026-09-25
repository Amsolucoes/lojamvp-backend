using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaHorarioChacara : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hora_entrada",
                table: "reservas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hora_saida",
                table: "reservas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "horarios_chacara",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    hora = table.Column<string>(type: "text", nullable: false),
                    ajuste = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_horarios_chacara", x => x.id);
                    table.ForeignKey(
                        name: "f_k_horarios_chacara_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5740));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5751));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5758));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5765));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5852));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5864));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5878));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5885));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5896));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5903));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5911));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5920));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5926));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(5933));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2985));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3174));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3413));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3567));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3732));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(3857));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4388));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2154));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4532));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4582));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4624));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4668));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4719));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4761));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4801));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4842));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4879));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(4918));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(1436), "$2a$11$2ya2zTODljcKGch9OoXujOkNOqaO/R84vEyqFb0TiP9kqlWJLLKAa" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 48, 28, 374, DateTimeKind.Utc).AddTicks(2022));

            migrationBuilder.CreateIndex(
                name: "i_x_horarios_chacara_loja_id_tipo_hora",
                table: "horarios_chacara",
                columns: new[] { "loja_id", "tipo", "hora" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horarios_chacara");

            migrationBuilder.DropColumn(
                name: "hora_entrada",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "hora_saida",
                table: "reservas");

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4606));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4617));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4624));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4630));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5476));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5490));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5502));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5509));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5523));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5529));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5536));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5543));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(5550));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(1650));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(1794));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(1998));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(2752));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(2919));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3038));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3151));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(1592));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3253));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3289));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3833));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3914));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(3981));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4034));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4077));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4123));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4162));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(4208));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(819), "$2a$11$8RTJUf4PjgpktlT0CIvMneU16SlAzH8AmKm2CrHJa8mmqfCdRr51K" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 25, 18, 18, 25, 430, DateTimeKind.Utc).AddTicks(1492));
        }
    }
}
