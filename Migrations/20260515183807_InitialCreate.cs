using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AmarTools.Bankcheck.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChequeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogoPath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChequeImagePath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PageWidth = table.Column<float>(type: "real", nullable: false),
                    PageHeight = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChequeTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChequeLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChequeTemplateId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    FontSize = table.Column<float>(type: "real", nullable: false),
                    FontStyle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FontColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaxLength = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChequeLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChequeLayouts_ChequeTemplates_ChequeTemplateId",
                        column: x => x.ChequeTemplateId,
                        principalTable: "ChequeTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrintedCheques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChequeTemplateId = table.Column<int>(type: "integer", nullable: false),
                    PayeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountInWords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChequeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChequeNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MemoNote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrintedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintedCheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintedCheques_ChequeTemplates_ChequeTemplateId",
                        column: x => x.ChequeTemplateId,
                        principalTable: "ChequeTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ChequeTemplates",
                columns: new[] { "Id", "AccountNumber", "BankName", "ChequeImagePath", "CreatedAt", "IsActive", "LogoPath", "PageHeight", "PageWidth" },
                values: new object[] { 1, "1234567890", "City Bank", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, 300f, 842f });

            migrationBuilder.InsertData(
                table: "ChequeLayouts",
                columns: new[] { "Id", "ChequeTemplateId", "FieldName", "FontColor", "FontSize", "FontStyle", "MaxLength", "X", "Y" },
                values: new object[,]
                {
                    { 1, 1, "Payee", "#000000", 11f, "Normal", 100, 140f, 185f },
                    { 2, 1, "Amount", "#000000", 11f, "Bold", 20, 660f, 185f },
                    { 3, 1, "AmountWords", "#000000", 10f, "Normal", 200, 90f, 155f },
                    { 4, 1, "Date", "#000000", 10f, "Normal", 20, 660f, 240f },
                    { 5, 1, "ChequeNo", "#555555", 9f, "Normal", 30, 90f, 55f }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChequeLayouts_ChequeTemplateId",
                table: "ChequeLayouts",
                column: "ChequeTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintedCheques_ChequeTemplateId",
                table: "PrintedCheques",
                column: "ChequeTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChequeLayouts");

            migrationBuilder.DropTable(
                name: "PrintedCheques");

            migrationBuilder.DropTable(
                name: "ChequeTemplates");
        }
    }
}
