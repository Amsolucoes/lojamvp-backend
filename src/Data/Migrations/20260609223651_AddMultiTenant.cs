using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lojas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cor_primaria = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    trial_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    mensalidade_dia = table.Column<int>(type: "integer", nullable: false),
                    mensalidade_valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ultima_cobranca = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    proximo_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    schema_nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_lojas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pago_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    forma_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    mp_payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mp_qr_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mp_qr_code_base64 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mp_boleto_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mp_boleto_barcode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registrado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "f_k_pagamentos_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_loja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_usuarios_loja", x => x.id);
                    table.ForeignKey(
                        name: "f_k_usuarios_loja_lojas_loja_id",
                        column: x => x.loja_id,
                        principalTable: "lojas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_usuarios_loja_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 9, 22, 36, 50, 394, DateTimeKind.Utc).AddTicks(4963), "$2a$11$5LU.EwMFBMcCucIlbfMmfuN/wCEDZblP33SrWahiEBwURxCVdiCXi" });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "ativo", "criado_em", "email", "nome", "role", "senha_hash" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), true, new DateTime(2026, 6, 9, 22, 36, 50, 547, DateTimeKind.Utc).AddTicks(4323), "superadmin@suaempresa.com", "Super Admin", "superadmin", "$2a$11$l6/9.dzOcRt8H9x9TCDXqeBObdTGt1ZHq/i6EB/Jo2Z5JKA3kdm0." });

            migrationBuilder.CreateIndex(
                name: "i_x_lojas_email",
                table: "lojas",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_lojas_schema_nome",
                table: "lojas",
                column: "schema_nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_pagamentos_loja_id",
                table: "pagamentos",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_usuarios_loja_loja_id",
                table: "usuarios_loja",
                column: "loja_id");

            migrationBuilder.CreateIndex(
                name: "i_x_usuarios_loja_usuario_id",
                table: "usuarios_loja",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropTable(
                name: "usuarios_loja");

            migrationBuilder.DropTable(
                name: "lojas");

            migrationBuilder.DeleteData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 5, 21, 17, 53, 484, DateTimeKind.Utc).AddTicks(933), "$2a$11$wfKbtERpJRzzZQwwXJRCE.6KF3IDEWACv2.6peGlcR0Yg853Lc/PC" });
        }
    }
}
