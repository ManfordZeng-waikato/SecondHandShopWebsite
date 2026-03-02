using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserLoginFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ProductImages",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "AdminUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AdminUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "AdminUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_UserName",
                table: "AdminUsers",
                column: "UserName",
                unique: true,
                filter: "[UserName] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_UserName",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "AdminUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ProductImages",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldDefaultValue: "");
        }
    }
}
