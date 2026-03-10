using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaELearning.Migrations
{
    /// <inheritdoc />
    public partial class AddCursosYContenidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaestroId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cursos_Users_MaestroId",
                        column: x => x.MaestroId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContenidosCursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ContenidoTexto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RutaArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlVideo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContenidosCursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContenidosCursos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContenidoCurso_CursoId",
                table: "ContenidosCursos",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContenidoCurso_Tipo",
                table: "ContenidosCursos",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Curso_MaestroId",
                table: "Cursos",
                column: "MaestroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContenidosCursos");

            migrationBuilder.DropTable(
                name: "Cursos");
        }
    }
}
