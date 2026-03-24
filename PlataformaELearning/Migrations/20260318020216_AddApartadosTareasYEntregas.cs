using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaELearning.Migrations
{
    /// <inheritdoc />
    public partial class AddApartadosTareasYEntregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "Calificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCalificacion",
                table: "Calificaciones",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TareaId",
                table: "Calificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApartadosCursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApartadosCursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApartadosCursos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TareasApartados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApartadoId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoTarea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaLimite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PuntosTotales = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasApartados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareasApartados_ApartadosCursos_ApartadoId",
                        column: x => x.ApartadoId,
                        principalTable: "ApartadosCursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregasTareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    AlumnoId = table.Column<int>(type: "int", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComentarioAlumno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoEntregado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalificacionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasTareas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasTareas_Calificaciones_CalificacionId",
                        column: x => x.CalificacionId,
                        principalTable: "Calificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EntregasTareas_TareasApartados_TareaId",
                        column: x => x.TareaId,
                        principalTable: "TareasApartados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntregasTareas_Users_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ContenidoTexto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoFisico = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlVideo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesTarea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialesTarea_TareasApartados_TareaId",
                        column: x => x.TareaId,
                        principalTable: "TareasApartados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_TareaId",
                table: "Calificaciones",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_Apartado_CursoId",
                table: "ApartadosCursos",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrega_AlumnoId",
                table: "EntregasTareas",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrega_TareaId",
                table: "EntregasTareas",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasTareas_CalificacionId",
                table: "EntregasTareas",
                column: "CalificacionId",
                unique: true,
                filter: "[CalificacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesTarea_TareaId",
                table: "MaterialesTarea",
                column: "TareaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarea_ApartadoId",
                table: "TareasApartados",
                column: "ApartadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarea_FechaLimite",
                table: "TareasApartados",
                column: "FechaLimite");

            migrationBuilder.AddForeignKey(
                name: "FK_Calificaciones_TareasApartados_TareaId",
                table: "Calificaciones",
                column: "TareaId",
                principalTable: "TareasApartados",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calificaciones_TareasApartados_TareaId",
                table: "Calificaciones");

            migrationBuilder.DropTable(
                name: "EntregasTareas");

            migrationBuilder.DropTable(
                name: "MaterialesTarea");

            migrationBuilder.DropTable(
                name: "TareasApartados");

            migrationBuilder.DropTable(
                name: "ApartadosCursos");

            migrationBuilder.DropIndex(
                name: "IX_Calificaciones_TareaId",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "EntregaId",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "FechaCalificacion",
                table: "Calificaciones");

            migrationBuilder.DropColumn(
                name: "TareaId",
                table: "Calificaciones");
        }
    }
}
