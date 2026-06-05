using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    public partial class RemoveCpfIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_clientes_cpf;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS i_x_clientes_cpf;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}