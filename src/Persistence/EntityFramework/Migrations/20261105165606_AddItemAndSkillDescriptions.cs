using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAndSkillDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "config",
                table: "Skill",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "config",
                table: "ItemDefinition",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "config",
                table: "Skill");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "config",
                table: "ItemDefinition");
        }
    }
}
