using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaOrdemServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checklist_categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_checklist_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamentos_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aprovado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lancamento_financeiro_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_orcamentos_servico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "checklist_itens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_checklist_itens", x => x.id);
                    table.ForeignKey(
                        name: "f_k_checklist_itens_checklist_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "checklist_categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_orcamento_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_orcamento_servico", x => x.id);
                    table.ForeignKey(
                        name: "f_k_itens_orcamento_servico__orcamentos_servico_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_itens_orcamento_servico_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mecanicos_orcamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comissao_percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_mecanicos_orcamento", x => x.id);
                    table.ForeignKey(
                        name: "f_k_mecanicos_orcamento__orcamentos_servico_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_mecanicos_orcamento_profissionais_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "profissionais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_respostas_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_checklist_respostas_item", x => x.id);
                    table.ForeignKey(
                        name: "f_k_checklist_respostas_item__orcamentos_servico_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_checklist_respostas_item_checklist_itens_checklist_item_id",
                        column: x => x.checklist_item_id,
                        principalTable: "checklist_itens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5439));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5443));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5446));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5451));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5506));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5511));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5515));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5518));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5521));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5524));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5527));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5533));

            migrationBuilder.InsertData(
                table: "modulos_preco",
                columns: new[] { "id", "atualizado_em", "chave", "disponivel_para_ativar", "nome", "valor" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111109"), new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5536), "ordem_servico", false, "Ordem de Serviço (oficina/auto peças)", 49.90m });

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(3931));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4059));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4383));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4604));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4694));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(3879));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4787));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4822));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4859));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4895));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(4928));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5041));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5074));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5108));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5140));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(5199));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(3220), "$2a$11$b/tpeZ7o/QYnpAgjCHjw/OpHCaQF3veAl0v8LRiWeYqqFbticJiAq" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 17, 29, 603, DateTimeKind.Utc).AddTicks(3787));

            migrationBuilder.CreateIndex(
                name: "i_x_comissoes_funcionario_origem_tipo_origem_id",
                table: "comissoes_funcionario",
                columns: new[] { "origem_tipo", "origem_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_checklist_itens_categoria_id",
                table: "checklist_itens",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "i_x_checklist_respostas_item_checklist_item_id",
                table: "checklist_respostas_item",
                column: "checklist_item_id");

            migrationBuilder.CreateIndex(
                name: "i_x_checklist_respostas_item_orcamento_id",
                table: "checklist_respostas_item",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_orcamento_servico_orcamento_id",
                table: "itens_orcamento_servico",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_orcamento_servico_produto_id",
                table: "itens_orcamento_servico",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "i_x_mecanicos_orcamento_orcamento_id",
                table: "mecanicos_orcamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "i_x_mecanicos_orcamento_profissional_id",
                table: "mecanicos_orcamento",
                column: "profissional_id");

            migrationBuilder.CreateIndex(
                name: "i_x_orcamentos_servico_loja_id_status",
                table: "orcamentos_servico",
                columns: new[] { "loja_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_respostas_item");

            migrationBuilder.DropTable(
                name: "itens_orcamento_servico");

            migrationBuilder.DropTable(
                name: "mecanicos_orcamento");

            migrationBuilder.DropTable(
                name: "checklist_itens");

            migrationBuilder.DropTable(
                name: "orcamentos_servico");

            migrationBuilder.DropTable(
                name: "checklist_categorias");

            migrationBuilder.DropIndex(
                name: "i_x_comissoes_funcionario_origem_tipo_origem_id",
                table: "comissoes_funcionario");

            migrationBuilder.DeleteData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9598));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9601));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9603));

            migrationBuilder.UpdateData(
                table: "categorias_acessorio",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9605));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9648));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9651));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9655));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9657));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9660));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9662));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9664));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "atualizado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9667));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8528));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8748));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8888));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8973));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9035));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9095));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8492));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9188));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9212));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9237));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9260));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9286));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9308));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9331));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9354));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9376));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(9399));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(7878), "$2a$11$Df7xfMa6DtC7uqALWzANkOMHjso7QnwbRtA4O5Ig32ItnuVTiTmjW" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 8, 21, 21, 7, 36, 707, DateTimeKind.Utc).AddTicks(8413));
        }
    }
}
