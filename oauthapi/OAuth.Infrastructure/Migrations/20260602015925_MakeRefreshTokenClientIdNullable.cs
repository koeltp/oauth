using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeRefreshTokenClientIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Clients_ClientId",
                table: "RefreshTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "RefreshTokens",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Clients_ClientId",
                table: "RefreshTokens",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Clients_ClientId",
                table: "RefreshTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "RefreshTokens",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Clients_ClientId",
                table: "RefreshTokens",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
