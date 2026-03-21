using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAccessPointsAndRemoveRuleEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeAccessPoints",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessPointId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAccessPoints", x => new { x.EmployeeId, x.AccessPointId });
                    table.ForeignKey(
                        name: "FK_EmployeeAccessPoints_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAccessPoints_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccessPoints_AccessPointId",
                table: "EmployeeAccessPoints",
                column: "AccessPointId");

            migrationBuilder.Sql(@"
                INSERT INTO ""EmployeeAccessPoints"" (""EmployeeId"", ""AccessPointId"")
                SELECT DISTINCT ""EmployeeId"", ""AccessPointId""
                FROM ""AccessRules""
                WHERE ""EmployeeId"" IS NOT NULL AND ""AccessPointId"" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessRules_Employees_EmployeeId",
                table: "AccessRules");

            migrationBuilder.DropIndex(
                name: "IX_AccessRules_EmployeeId",
                table: "AccessRules");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AccessRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "AccessRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""AccessRules"" AS ar
                SET ""EmployeeId"" = eap.""EmployeeId""
                FROM (
                    SELECT DISTINCT ON (""AccessPointId"") ""AccessPointId"", ""EmployeeId""
                    FROM ""EmployeeAccessPoints""
                    ORDER BY ""AccessPointId"", ""EmployeeId""
                ) AS eap
                WHERE ar.""AccessPointId"" = eap.""AccessPointId"";");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_EmployeeId",
                table: "AccessRules",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessRules_Employees_EmployeeId",
                table: "AccessRules",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "EmployeeAccessPoints");
        }
    }
}