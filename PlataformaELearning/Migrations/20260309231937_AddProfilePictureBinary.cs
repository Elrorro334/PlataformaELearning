using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaELearning.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureBinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePicturePath",
                table: "Users",
                newName: "ContentType");

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Users",
                newName: "ProfilePicturePath");
        }
    }
}
