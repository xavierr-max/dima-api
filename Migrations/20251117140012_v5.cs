using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class v5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_IdentityUser_NormalizedUserName",
                table: "IdentityUser",
                newName: "UserNameIndex");

            migrationBuilder.RenameIndex(
                name: "IX_IdentityUser_NormalizedEmail",
                table: "IdentityUser",
                newName: "EmailIndex");

            migrationBuilder.RenameIndex(
                name: "IX_IdentityRole_NormalizedName",
                table: "IdentityRole",
                newName: "RoleNameIndex");

            migrationBuilder.CreateTable(
                name: "IdentityPasskeyData",
                columns: table => new
                {
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignCount = table.Column<long>(type: "bigint", nullable: false),
                    Transports = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsUserVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsBackupEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "bit", nullable: false),
                    AttestationObject = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ClientDataJson = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "UserPasskeys",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "varbinary(900)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasskeys", x => new { x.UserId, x.CredentialId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityUserRole_RoleId",
                table: "IdentityUserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRoleClaim_RoleId",
                table: "IdentityRoleClaim",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityRoleClaim_IdentityRole_RoleId",
                table: "IdentityRoleClaim",
                column: "RoleId",
                principalTable: "IdentityRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityUserRole_IdentityRole_RoleId",
                table: "IdentityUserRole",
                column: "RoleId",
                principalTable: "IdentityRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdentityRoleClaim_IdentityRole_RoleId",
                table: "IdentityRoleClaim");

            migrationBuilder.DropForeignKey(
                name: "FK_IdentityUserRole_IdentityRole_RoleId",
                table: "IdentityUserRole");

            migrationBuilder.DropTable(
                name: "IdentityPasskeyData");

            migrationBuilder.DropTable(
                name: "UserPasskeys");

            migrationBuilder.DropIndex(
                name: "IX_IdentityUserRole_RoleId",
                table: "IdentityUserRole");

            migrationBuilder.DropIndex(
                name: "IX_IdentityRoleClaim_RoleId",
                table: "IdentityRoleClaim");

            migrationBuilder.RenameIndex(
                name: "UserNameIndex",
                table: "IdentityUser",
                newName: "IX_IdentityUser_NormalizedUserName");

            migrationBuilder.RenameIndex(
                name: "EmailIndex",
                table: "IdentityUser",
                newName: "IX_IdentityUser_NormalizedEmail");

            migrationBuilder.RenameIndex(
                name: "RoleNameIndex",
                table: "IdentityRole",
                newName: "IX_IdentityRole_NormalizedName");
        }
    }
}
