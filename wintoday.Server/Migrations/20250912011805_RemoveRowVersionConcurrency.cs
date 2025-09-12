using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wintoday.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRowVersionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BetCommitted",
                table: "Rounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Rounds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Players",
                type: "bytea",
                nullable: false,
                defaultValueSql: "'\\x'::bytea",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_PlayerId",
                table: "Rounds",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_Players_PlayerId",
                table: "Rounds",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_Players_PlayerId",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_PlayerId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "BetCommitted",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Rounds");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Players",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldDefaultValueSql: "'\\x'::bytea");
        }
    }
}
