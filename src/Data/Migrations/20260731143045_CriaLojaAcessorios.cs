using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriaLojaAcessorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pedidos_acessorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cliente_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cliente_telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cliente_cpf_cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    cep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    endereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_frete = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo_rastreio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mp_payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mp_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    enviado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_pedidos_acessorio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos_acessorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    preco_promocional = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    estoque = table.Column<int>(type: "integer", nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    imagens_urls = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    peso_kg = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_produtos_acessorio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens_pedido_acessorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_produto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_pedido_acessorio", x => x.id);
                    table.ForeignKey(
                        name: "f_k_itens_pedido_acessorio__pedidos_acessorio_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos_acessorio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_itens_pedido_acessorio__produtos_acessorio_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos_acessorio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1781));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1785));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1787));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1791));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1793));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1796));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(652));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(775));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(983));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1176));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1236));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(613));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1296));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1318));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1340));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1361));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1402));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1445));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1466));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1488));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(1563));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(5), "$2a$11$wiyCWTYt9mrZPcLOYyK4Xej8trKcqdpUavNSLLzMFTJ.G9ObTQo5y" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 31, 14, 30, 44, 32, DateTimeKind.Utc).AddTicks(528));

            migrationBuilder.CreateIndex(
                name: "i_x_itens_pedido_acessorio_pedido_id",
                table: "itens_pedido_acessorio",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_pedido_acessorio_produto_id",
                table: "itens_pedido_acessorio",
                column: "produto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_pedido_acessorio");

            migrationBuilder.DropTable(
                name: "pedidos_acessorio");

            migrationBuilder.DropTable(
                name: "produtos_acessorio");

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8630));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8635));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8639));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8641));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8643));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8646));

            migrationBuilder.UpdateData(
                table: "modulos_preco",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "atualizado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8650));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7664));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7754));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7976));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8057));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8118));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8176));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7628));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8236));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8257));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8279));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8329));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8350));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8371));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8394));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8415));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8437));

            migrationBuilder.UpdateData(
                table: "perfis_loja",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-00000000000a"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(8458));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7072), "$2a$11$EzDcrSJQHtY1FaaRru38QewyQXe.ut.LdDRReckLZGaBg6lowzyTa" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "criado_em",
                value: new DateTime(2026, 7, 30, 12, 57, 3, 361, DateTimeKind.Utc).AddTicks(7548));
        }
    }
}
