using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrenaAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnderecoValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ENDERECO",
                table: "TB_DRENA_OCORRENCIA",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Local marcado no mapa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ENDERECO",
                table: "TB_DRENA_OCORRENCIA");
        }
    }
}
