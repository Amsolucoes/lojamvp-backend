using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarIndiceCodigoBarras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove índices antigos, só se existirem (seguro em qualquer ambiente)
            migrationBuilder.Sql("DROP INDEX IF EXISTS i_x_produtos_codigo_barras;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS i_x_produtos_loja_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_produtos_codigo_barras;");

            // Cria o índice composto (loja_id + codigo_barras), único por loja
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_produtos_codigo_barras
                ON produtos (loja_id, codigo_barras)
                WHERE (codigo_barras IS NOT NULL AND codigo_barras <> '');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_produtos_codigo_barras;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS i_x_produtos_codigo_barras
                ON produtos (codigo_barras)
                WHERE (codigo_barras IS NOT NULL);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS i_x_produtos_loja_id
                ON produtos (loja_id);
            ");
        }
    }
}