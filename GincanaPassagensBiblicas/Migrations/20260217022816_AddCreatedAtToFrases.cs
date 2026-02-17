using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GincanaPassagensBiblicas.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToFrases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Frases",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Frases");
        }
    }
}
