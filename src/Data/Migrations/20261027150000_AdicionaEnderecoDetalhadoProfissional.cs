using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027150000_AdicionaEnderecoDetalhadoProfissional")]
    public partial class AdicionaEnderecoDetalhadoProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "numero", table: "profissionais", type: "character varying(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<string>(name: "bairro", table: "profissionais", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "cidade", table: "profissionais", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "uf", table: "profissionais", type: "character varying(2)", maxLength: 2, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "numero", table: "profissionais");
            migrationBuilder.DropColumn(name: "bairro", table: "profissionais");
            migrationBuilder.DropColumn(name: "cidade", table: "profissionais");
            migrationBuilder.DropColumn(name: "uf", table: "profissionais");
        }
    }
}
