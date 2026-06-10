using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LojaApi.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixQrCodeLenth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "mp_qr_code_base64",
                table: "pagamentos",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mp_qr_code",
                table: "pagamentos",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 10, 21, 30, 40, 465, DateTimeKind.Utc).AddTicks(196), "$2a$11$x8yJgA2M3MU6G6DUVUexyOlhYkqGauf71fjMatQk3npxkzZrM6Om." });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 10, 21, 30, 40, 612, DateTimeKind.Utc).AddTicks(6829), "$2a$11$CGaeLrB0/ky/BSKg72FJ1uNQ3CI6lRWs84SqAhlYORC5zm/rot5iW" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "mp_qr_code_base64",
                table: "pagamentos",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mp_qr_code",
                table: "pagamentos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 9, 22, 36, 50, 394, DateTimeKind.Utc).AddTicks(4963), "$2a$11$5LU.EwMFBMcCucIlbfMmfuN/wCEDZblP33SrWahiEBwURxCVdiCXi" });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "criado_em", "senha_hash" },
                values: new object[] { new DateTime(2026, 6, 9, 22, 36, 50, 547, DateTimeKind.Utc).AddTicks(4323), "$2a$11$l6/9.dzOcRt8H9x9TCDXqeBObdTGt1ZHq/i6EB/Jo2Z5JKA3kdm0." });
        }
    }
}
