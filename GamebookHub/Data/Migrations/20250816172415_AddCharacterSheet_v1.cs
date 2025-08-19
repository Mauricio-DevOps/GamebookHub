using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamebookHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSheet_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterSheetId",
                table: "Gamebooks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Inventory_Id = table.Column<int>(type: "int", nullable: false),
                    Inventory_Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Inventory_Mode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Inventory_Slots = table.Column<int>(type: "int", nullable: true),
                    Inventory_Capacity = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Min = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Max = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Default = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EnumOptions = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CharacterSheetTemplateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeDefinitions_CharacterSheets_CharacterSheetTemplateId",
                        column: x => x.CharacterSheetTemplateId,
                        principalTable: "CharacterSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gamebooks_CharacterSheetId",
                table: "Gamebooks",
                column: "CharacterSheetId",
                unique: true,
                filter: "[CharacterSheetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_CharacterSheetTemplateId_Key",
                table: "AttributeDefinitions",
                columns: new[] { "CharacterSheetTemplateId", "Key" },
                unique: true,
                filter: "[CharacterSheetTemplateId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Gamebooks_CharacterSheets_CharacterSheetId",
                table: "Gamebooks",
                column: "CharacterSheetId",
                principalTable: "CharacterSheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gamebooks_CharacterSheets_CharacterSheetId",
                table: "Gamebooks");

            migrationBuilder.DropTable(
                name: "AttributeDefinitions");

            migrationBuilder.DropTable(
                name: "CharacterSheets");

            migrationBuilder.DropIndex(
                name: "IX_Gamebooks_CharacterSheetId",
                table: "Gamebooks");

            migrationBuilder.DropColumn(
                name: "CharacterSheetId",
                table: "Gamebooks");
        }
    }
}
