using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrenaAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_DRENA_OCORRENCIA",
                columns: table => new
                {
                    ID_OCORRENCIA = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ORIGEM = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    DESCRICAO_SANITIZADA = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    FOTO_URL = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    LATITUDE = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    LONGITUDE = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    SEVERIDADE = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    IPC = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    CRIADA_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    ID_USUARIO = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_DRENA_OCORRENCIA", x => x.ID_OCORRENCIA);
                });

            migrationBuilder.CreateTable(
                name: "TB_DRENA_USUARIO",
                columns: table => new
                {
                    ID_USUARIO = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    NOME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(180)", maxLength: 180, nullable: false),
                    SENHA_HASH = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_DRENA_USUARIO", x => x.ID_USUARIO);
                });

            migrationBuilder.CreateTable(
                name: "TB_DRENA_HISTORICO_STATUS",
                columns: table => new
                {
                    ID_HISTORICO = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ID_OCORRENCIA = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    DATA_HORA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_DRENA_HISTORICO_STATUS", x => x.ID_HISTORICO);
                    table.ForeignKey(
                        name: "FK_TB_DRENA_HISTORICO_STATUS_TB_DRENA_OCORRENCIA_ID_OCORRENCIA",
                        column: x => x.ID_OCORRENCIA,
                        principalTable: "TB_DRENA_OCORRENCIA",
                        principalColumn: "ID_OCORRENCIA",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_DRENA_HISTORICO_STATUS_ID_OCORRENCIA",
                table: "TB_DRENA_HISTORICO_STATUS",
                column: "ID_OCORRENCIA");

            migrationBuilder.CreateIndex(
                name: "IX_TB_DRENA_USUARIO_EMAIL",
                table: "TB_DRENA_USUARIO",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_DRENA_HISTORICO_STATUS");

            migrationBuilder.DropTable(
                name: "TB_DRENA_USUARIO");

            migrationBuilder.DropTable(
                name: "TB_DRENA_OCORRENCIA");
        }
    }
}
