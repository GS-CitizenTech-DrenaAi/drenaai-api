using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrenaAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTabelaSensorBueiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_DRENA_SENSOR_BUEIRO",
                columns: table => new
                {
                    ID_SENSOR_BUEIRO = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CODIGO_SENSOR = table.Column<string>(type: "NVARCHAR2(60)", maxLength: 60, nullable: false),
                    ENDERECO = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    LATITUDE = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    LONGITUDE = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    DISTANCIA_CM = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    GAS_PPM = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    OBSTRUCAO_PERCENTUAL = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    GAS_DETECTADO = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    DT_ULTIMA_LEITURA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    ID_OCORRENCIA_ATIVA = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_DRENA_SENSOR_BUEIRO", x => x.ID_SENSOR_BUEIRO);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_DRENA_SENSOR_BUEIRO");
        }
    }
}
