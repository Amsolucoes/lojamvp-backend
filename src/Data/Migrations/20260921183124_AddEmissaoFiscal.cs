using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmissaoFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracoes_fiscais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    provedor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ambiente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    credencial_token = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    inscricao_estadual = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    regime_tributario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    serie_nfce = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    proximo_numero_nfce = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_configuracoes_fiscais", x => x.id);
                    table.ForeignKey(
                        name: "f_k_configuracoes_fiscais_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emissoes_fiscais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provedor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    chave_acesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    numero_nota = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    serie_nota = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    protocolo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    url_danfe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    url_xml = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    motivo_erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    solicitada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    solicitada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_emissoes_fiscais", x => x.id);
                    table.ForeignKey(
                        name: "f_k_emissoes_fiscais_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_emissoes_fiscais_vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6490));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6501));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6509));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6518));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6615));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6628));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6642));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6651));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6658));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6666));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6674));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6685));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "atualizado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6692));

            migrationBuilder.InsertData(
                table: "modulos_preco",
                columns: new[] { "id", "atualizado_em", "chave", "disponivel_para_ativar", "nome", "valor" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111110"), new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(6699), "nfce", false, "Emissão de Nota Fiscal (NFC-e)", 49.90m });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(2860));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(3006));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(3224));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(4400));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(4605));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(4861));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(2793));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5007));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5072));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5108));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5146));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5209));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5314));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5356));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5792));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5858));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(5907));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(1860), "$2a$11$jqe/FBmCERbxTdji9fS51uZFN2MD0hdD1uG5bxik5Z8x9YmS3.1hC" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 9, 21, 18, 31, 22, 821, DateTimeKind.Utc).AddTicks(2635));

            migrationBuilder.CreateIndex(
                name: "i_x_configuracoes_fiscais_loja_id",
                table: "configuracoes_fiscais",
                column: "loja_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_emissoes_fiscais_loja_id_venda_id",
                table: "emissoes_fiscais",
                columns: new[] { "loja_id", "venda_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_emissoes_fiscais_venda_id",
                table: "emissoes_fiscais",
                column: "venda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_fiscais");

            migrationBuilder.DropTable(
                name: "emissoes_fiscais");

            migrationBuilder.DeleteData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"));

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
        }
    }
}
