using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Participant"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_Role", "[Role] in ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                    table.CheckConstraint("CK_Events_EventType", "[EventType] in ('Running', 'Cycling', 'Walking')");
                    table.ForeignKey(
                        name: "FK_Events_Users_OrganiserID",
                        column: x => x.OrganiserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0.00m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                    table.CheckConstraint("CK_Categories_DistanceKm", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
                    table.CheckConstraint("CK_Categories_MaxParticipants", "[MaxParticipants] > 0");
                    table.ForeignKey(
                        name: "FK_Categories_Events_EventID",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventEnrolments",
                columns: table => new
                {
                    EnrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    BibNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEnrolments", x => x.EnrolmentID);
                    table.CheckConstraint("CK_EventEnrolments_Status", "[Status] in ('Active', 'Cancelled', 'Completed')");
                    table.ForeignKey(
                        name: "FK_EventEnrolments_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventEnrolments_Users_ParticipantID",
                        column: x => x.ParticipantID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ElevationGain = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MapUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteID);
                    table.CheckConstraint("CK_Routes_DistanceKm", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Routes_ElevationGain", "[ElevationGain] >= 0");
                    table.ForeignKey(
                        name: "FK_Routes_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentID = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CapturedByUserID = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultID);
                    table.CheckConstraint("CK_Results_Position", "[Position] > 0");
                    table.ForeignKey(
                        name: "FK_Results_EventEnrolments_EnrolmentID",
                        column: x => x.EnrolmentID,
                        principalTable: "EventEnrolments",
                        principalColumn: "EnrolmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Results_Users_CapturedByUserID",
                        column: x => x.CapturedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventID_Name",
                table: "Categories",
                columns: new[] { "EventID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_BibNumber",
                table: "EventEnrolments",
                column: "BibNumber",
                unique: true,
                filter: "[BibNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryID",
                table: "EventEnrolments",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_ParticipantID_CategoryID",
                table: "EventEnrolments",
                columns: new[] { "ParticipantID", "CategoryID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserID",
                table: "Events",
                column: "OrganiserID");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CapturedByUserID",
                table: "Results",
                column: "CapturedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrolmentID",
                table: "Results",
                column: "EnrolmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_CategoryID",
                table: "Routes",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "EventEnrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
