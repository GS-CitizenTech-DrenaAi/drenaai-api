using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrenaAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmacoesOcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CONFIRMACOES",
                table: "TB_DRENA_OCORRENCIA",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CONFIRMACOES",
                table: "TB_DRENA_OCORRENCIA");
        }
    }
}
