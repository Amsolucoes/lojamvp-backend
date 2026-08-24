using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPatrimonio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contagens_patrimonio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_contagem = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responsavel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_contagens_patrimonio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens_patrimonio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    quantidade_esperada = table.Column<int>(type: "integer", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_patrimonio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens_contagem_patrimonio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_patrimonio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade_esperada_no_momento = table.Column<int>(type: "integer", nullable: false),
                    quantidade_contada = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_contagem_patrimonio", x => x.id);
                    table.ForeignKey(
                        name: "f_k_itens_contagem_patrimonio__itens_patrimonio_item_patrimonio_id",
                        column: x => x.item_patrimonio_id,
                        principalTable: "itens_patrimonio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_itens_contagem_patrimonio_contagens_patrimonio_contagem_id",
                        column: x => x.contagem_id,
                        principalTable: "contagens_patrimonio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9531));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9534));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9537));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9539));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9587));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9590));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9596));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9598));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9601));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9689));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9692));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9695));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9697));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(7369));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(7474));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(7822));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(7926));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(8012));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(8086));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(7221));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(8162));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(8190));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(8218));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9025));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9166));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9231));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9260));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9287));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(9314));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(2586), "$2a$11$FX28236HuhlD/Ix/1TZUcOgLu0B6tTOVkuBagcIHFcAr33mVJZJWm" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 49, 35, 378, DateTimeKind.Utc).AddTicks(6271));

            migrationBuilder.CreateIndex(
                name: "i_x_contagens_patrimonio_loja_id_data_contagem",
                table: "contagens_patrimonio",
                columns: new[] { "loja_id", "data_contagem" });

            migrationBuilder.CreateIndex(
                name: "i_x_itens_contagem_patrimonio_contagem_id",
                table: "itens_contagem_patrimonio",
                column: "contagem_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_contagem_patrimonio_item_patrimonio_id",
                table: "itens_contagem_patrimonio",
                column: "item_patrimonio_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_patrimonio_loja_id_ativo",
                table: "itens_patrimonio",
                columns: new[] { "loja_id", "ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_contagem_patrimonio");

            migrationBuilder.DropTable(
                name: "itens_patrimonio");

            migrationBuilder.DropTable(
                name: "contagens_patrimonio");

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3318));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3322));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3325));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3328));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3365));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3369));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3373));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3376));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3378));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3381));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3385));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3424));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3426));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2298));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2378));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2611));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2698));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2760));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2821));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2243));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2882));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2904));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2929));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2951));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3020));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3042));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3064));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3087));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3127));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(3150));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(1806), "$2a$11$Zp4s3ICqdXJZWA5Kymo7nuIODJ95ToyWhc1.APtXD3AYyg9/gRpW6" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 24, 14, 14, 42, 0, DateTimeKind.Utc).AddTicks(2200));
        }
    }
}
