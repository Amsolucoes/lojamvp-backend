using LojaApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261017120000_AdicionaCredencialMpPorLoja")]
    public partial class AdicionaCredencialMpPorLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mp_access_token_cifrado",
                table: "infos_chacara",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mp_public_key",
                table: "infos_chacara",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "mp_conta_plataforma",
                table: "infos_chacara",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // A chácara mais antiga é a da própria plataforma: continua recebendo na conta do servidor.
            // As demais precisam cadastrar a própria conta.
            migrationBuilder.Sql("UPDATE infos_chacara SET mp_conta_plataforma = true WHERE id = (SELECT MIN(id) FROM infos_chacara);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "mp_access_token_cifrado", table: "infos_chacara");
            migrationBuilder.DropColumn(name: "mp_public_key", table: "infos_chacara");
            migrationBuilder.DropColumn(name: "mp_conta_plataforma", table: "infos_chacara");
        }
    }
}
