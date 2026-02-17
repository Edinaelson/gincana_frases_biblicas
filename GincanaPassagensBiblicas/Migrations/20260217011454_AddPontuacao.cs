using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GincanaPassagensBiblicas.Migrations
{
    /// <inheritdoc />
    public partial class AddPontuacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FraseId",
                table: "Pontuacoes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Frases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Pontuacoes_FraseId",
                table: "Pontuacoes",
                column: "FraseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pontuacoes_Frases_FraseId",
                table: "Pontuacoes",
                column: "FraseId",
                principalTable: "Frases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pontuacoes_Frases_FraseId",
                table: "Pontuacoes");

            migrationBuilder.DropIndex(
                name: "IX_Pontuacoes_FraseId",
                table: "Pontuacoes");

            migrationBuilder.DropColumn(
                name: "FraseId",
                table: "Pontuacoes");

            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Frases");
        }
    }
}
