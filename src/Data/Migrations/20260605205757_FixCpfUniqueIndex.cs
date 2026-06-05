using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCpfUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
               "DROP INDEX IF EXISTS ix_clientes_cpf");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_clientes_cpf ON clientes(cpf) WHERE cpf IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_clientes_cpf");
        }
    }
}
