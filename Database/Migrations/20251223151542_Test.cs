using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TbAuthority",
                columns: table => new
                {
                    Authority_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Authority_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbAuthority", x => x.Authority_ID);
                });

            migrationBuilder.CreateTable(
                name: "TbCitizen",
                columns: table => new
                {
                    Citizen_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Citizen_National_Id = table.Column<string>(type: "nchar(14)", fixedLength: true, maxLength: 14, nullable: false),
                    Citizen_Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Citizen_Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbCitizen", x => x.Citizen_ID);
                });

            migrationBuilder.CreateTable(
                name: "TbAuthority_Contact",
                columns: table => new
                {
                    Contact_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Contact_Info = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Authority_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbAuthority_Contact", x => x.Contact_Id);
                    table.ForeignKey(
                        name: "FK_TbAuthority_Contact_TbAuthority_Authority_ID",
                        column: x => x.Authority_ID,
                        principalTable: "TbAuthority",
                        principalColumn: "Authority_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TbCitizen_Phone",
                columns: table => new
                {
                    Phone_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phone_Number = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: false),
                    Citizen_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbCitizen_Phone", x => x.Phone_Id);
                    table.ForeignKey(
                        name: "FK_TbCitizen_Phone_TbCitizen_Citizen_ID",
                        column: x => x.Citizen_ID,
                        principalTable: "TbCitizen",
                        principalColumn: "Citizen_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TbReport",
                columns: table => new
                {
                    Report_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Report_Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Report_GeoLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Report_Submit = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Report_Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Report_PredictedCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Confidence_Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "In Progress"),
                    AiTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Citizen_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbReport", x => x.Report_ID);
                    table.ForeignKey(
                        name: "FK_TbReport_TbCitizen_Citizen_ID",
                        column: x => x.Citizen_ID,
                        principalTable: "TbCitizen",
                        principalColumn: "Citizen_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TbHandle",
                columns: table => new
                {
                    Report_ID = table.Column<int>(type: "int", nullable: false),
                    Authority_ID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Update_Report = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbHandle", x => new { x.Report_ID, x.Authority_ID });
                    table.ForeignKey(
                        name: "FK_TbHandle_TbAuthority_Authority_ID",
                        column: x => x.Authority_ID,
                        principalTable: "TbAuthority",
                        principalColumn: "Authority_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TbHandle_TbReport_Report_ID",
                        column: x => x.Report_ID,
                        principalTable: "TbReport",
                        principalColumn: "Report_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TbAuthority_Authority_Name_Department_Name",
                table: "TbAuthority",
                columns: new[] { "Authority_Name", "Department_Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbAuthority_Contact_Authority_ID_Contact_Info",
                table: "TbAuthority_Contact",
                columns: new[] { "Authority_ID", "Contact_Info" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbCitizen_Citizen_Email",
                table: "TbCitizen",
                column: "Citizen_Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbCitizen_Citizen_National_Id",
                table: "TbCitizen",
                column: "Citizen_National_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbCitizen_Phone_Citizen_ID",
                table: "TbCitizen_Phone",
                column: "Citizen_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TbCitizen_Phone_Phone_Number",
                table: "TbCitizen_Phone",
                column: "Phone_Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TbHandle_Authority_ID",
                table: "TbHandle",
                column: "Authority_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TbReport_Citizen_ID",
                table: "TbReport",
                column: "Citizen_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TbAuthority_Contact");

            migrationBuilder.DropTable(
                name: "TbCitizen_Phone");

            migrationBuilder.DropTable(
                name: "TbHandle");

            migrationBuilder.DropTable(
                name: "TbAuthority");

            migrationBuilder.DropTable(
                name: "TbReport");

            migrationBuilder.DropTable(
                name: "TbCitizen");
        }
    }
}
