using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamebookHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitGamebooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gamebooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gamebooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Playthroughs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GamebookId = table.Column<int>(type: "int", nullable: false),
                    CurrentNodeKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFinished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playthroughs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GamebookId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnding = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameNodes_Gamebooks_GamebookId",
                        column: x => x.GamebookId,
                        principalTable: "Gamebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromNodeId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToNodeKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresFlags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SetsFlags = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameChoices_GameNodes_FromNodeId",
                        column: x => x.FromNodeId,
                        principalTable: "GameNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gamebooks_Slug",
                table: "Gamebooks",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameChoices_FromNodeId",
                table: "GameChoices",
                column: "FromNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNodes_GamebookId_Key",
                table: "GameNodes",
                columns: new[] { "GamebookId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_UserId_GamebookId",
                table: "Playthroughs",
                columns: new[] { "UserId", "GamebookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameChoices");

            migrationBuilder.DropTable(
                name: "Playthroughs");

            migrationBuilder.DropTable(
                name: "GameNodes");

            migrationBuilder.DropTable(
                name: "Gamebooks");
        }
    }
}
