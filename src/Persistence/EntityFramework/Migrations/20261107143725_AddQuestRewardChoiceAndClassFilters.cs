// <copyright file="20261107143725_AddQuestRewardChoiceAndClassFilters.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddQuestRewardChoiceAndClassFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChoiceOption",
                schema: "config",
                table: "QuestReward",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "QuestDefinitionDisallowedCharacters",
                schema: "config",
                columns: table => new
                {
                    QuestDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterClassId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestDefinitionDisallowedCharacters", x => new { x.QuestDefinitionId, x.CharacterClassId });
                    table.ForeignKey(
                        name: "FK_QuestDefinitionDisallowedCharacters_CharacterClass_Characte~",
                        column: x => x.CharacterClassId,
                        principalSchema: "config",
                        principalTable: "CharacterClass",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestDefinitionDisallowedCharacters_QuestDefinition_QuestDe~",
                        column: x => x.QuestDefinitionId,
                        principalSchema: "config",
                        principalTable: "QuestDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestDefinitionQualifiedCharacters",
                schema: "config",
                columns: table => new
                {
                    QuestDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterClassId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestDefinitionQualifiedCharacters", x => new { x.QuestDefinitionId, x.CharacterClassId });
                    table.ForeignKey(
                        name: "FK_QuestDefinitionQualifiedCharacters_CharacterClass_Character~",
                        column: x => x.CharacterClassId,
                        principalSchema: "config",
                        principalTable: "CharacterClass",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestDefinitionQualifiedCharacters_QuestDefinition_QuestDef~",
                        column: x => x.QuestDefinitionId,
                        principalSchema: "config",
                        principalTable: "QuestDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestDefinitionDisallowedCharacters_CharacterClassId",
                schema: "config",
                table: "QuestDefinitionDisallowedCharacters",
                column: "CharacterClassId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestDefinitionQualifiedCharacters_CharacterClassId",
                schema: "config",
                table: "QuestDefinitionQualifiedCharacters",
                column: "CharacterClassId");

            // Preserve existing single-class filters as multi-allowed entries.
            migrationBuilder.Sql("""
                INSERT INTO config."QuestDefinitionQualifiedCharacters" ("QuestDefinitionId", "CharacterClassId")
                SELECT "Id", "QualifiedCharacterId"
                FROM config."QuestDefinition"
                WHERE "QualifiedCharacterId" IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_QuestDefinition_CharacterClass_QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition");

            migrationBuilder.DropIndex(
                name: "IX_QuestDefinition_QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition");

            migrationBuilder.DropColumn(
                name: "QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE config."QuestDefinition" q
                SET "QualifiedCharacterId" = j."CharacterClassId"
                FROM (
                    SELECT DISTINCT ON ("QuestDefinitionId") "QuestDefinitionId", "CharacterClassId"
                    FROM config."QuestDefinitionQualifiedCharacters"
                    ORDER BY "QuestDefinitionId", "CharacterClassId"
                ) j
                WHERE q."Id" = j."QuestDefinitionId";
                """);

            migrationBuilder.DropTable(
                name: "QuestDefinitionDisallowedCharacters",
                schema: "config");

            migrationBuilder.DropTable(
                name: "QuestDefinitionQualifiedCharacters",
                schema: "config");

            migrationBuilder.DropColumn(
                name: "IsChoiceOption",
                schema: "config",
                table: "QuestReward");

            migrationBuilder.CreateIndex(
                name: "IX_QuestDefinition_QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition",
                column: "QualifiedCharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestDefinition_CharacterClass_QualifiedCharacterId",
                schema: "config",
                table: "QuestDefinition",
                column: "QualifiedCharacterId",
                principalSchema: "config",
                principalTable: "CharacterClass",
                principalColumn: "Id");
        }
    }
}
