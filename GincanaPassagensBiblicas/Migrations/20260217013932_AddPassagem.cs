using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GincanaPassagensBiblicas.Migrations
{
    /// <inheritdoc />
    public partial class AddPassagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contexto",
                table: "Frases",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Passagem",
                table: "Frases",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contexto",
                table: "Frases");

            migrationBuilder.DropColumn(
                name: "Passagem",
                table: "Frases");
        }
    }
}
