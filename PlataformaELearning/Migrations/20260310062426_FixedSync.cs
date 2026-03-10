using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaELearning.Migrations
{
    public partial class FixedSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Borramos todo para que no intente crear tablas o columnas que ya existen
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Borramos todo aquí también
        }
    }
}