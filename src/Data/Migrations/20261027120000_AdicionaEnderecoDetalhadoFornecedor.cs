using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027120000_AdicionaEnderecoDetalhadoFornecedor")]
    public partial class AdicionaEnderecoDetalhadoFornecedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "cep", table: "fornecedores", type: "character varying(9)", maxLength: 9, nullable: true);
            migrationBuilder.AddColumn<string>(name: "rua", table: "fornecedores", type: "character varying(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<string>(name: "numero", table: "fornecedores", type: "character varying(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<string>(name: "bairro", table: "fornecedores", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "cidade", table: "fornecedores", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "uf", table: "fornecedores", type: "character varying(2)", maxLength: 2, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "cep", table: "fornecedores");
            migrationBuilder.DropColumn(name: "rua", table: "fornecedores");
            migrationBuilder.DropColumn(name: "numero", table: "fornecedores");
            migrationBuilder.DropColumn(name: "bairro", table: "fornecedores");
            migrationBuilder.DropColumn(name: "cidade", table: "fornecedores");
            migrationBuilder.DropColumn(name: "uf", table: "fornecedores");
        }
    }
}
