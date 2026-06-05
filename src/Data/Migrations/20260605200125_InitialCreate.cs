using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    preco_custo = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    preco_venda = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    estoque = table.Column<int>(type: "integer", nullable: false),
                    estoque_minimo = table.Column<int>(type: "integer", nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_produtos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    total_final = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    forma_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    troco = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_vendas", x => x.id);
                    table.ForeignKey(
                        name: "f_k_vendas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "movimentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_movimentos", x => x.id);
                    table.ForeignKey(
                        name: "f_k_movimentos__produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_venda",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_itens_venda", x => x.id);
                    table.ForeignKey(
                        name: "f_k_itens_venda__produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_itens_venda__vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "ativo", "criado_em", "email", "nome", "role", "senha_hash" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 6, 5, 20, 1, 25, 201, DateTimeKind.Utc).AddTicks(2057), "admin@loja.com", "Administrador", "admin", "$2a$11$XmWzJF7Cy9QdOd5c/Bf8iuOxFam2wWhvESmyfGVEeoJA1SHIyae3O" });

            migrationBuilder.CreateIndex(
                name: "i_x_clientes_cpf",
                table: "clientes",
                column: "cpf",
                unique: true,
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_venda_produto_id",
                table: "itens_venda",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "i_x_itens_venda_venda_id",
                table: "itens_venda",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "i_x_movimentos_produto_id",
                table: "movimentos",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "i_x_produtos_codigo_barras",
                table: "produtos",
                column: "codigo_barras",
                unique: true,
                filter: "codigo_barras IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_vendas_cliente_id",
                table: "vendas",
                column: "cliente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_venda");

            migrationBuilder.DropTable(
                name: "movimentos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "vendas");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
