using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndConcurrencyHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdempotentRequests_CreatedAt",
                table: "IdempotentRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "InventoryAssignments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "Inventories",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "IdempotentRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "IdempotentRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockExpiresAt",
                table: "IdempotentRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "IdempotentRequests",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotentRequests_ExpiresAt",
                table: "IdempotentRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotentRequests_LockExpiresAt",
                table: "IdempotentRequests",
                column: "LockExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdempotentRequests_ExpiresAt",
                table: "IdempotentRequests");

            migrationBuilder.DropIndex(
                name: "IX_IdempotentRequests_LockExpiresAt",
                table: "IdempotentRequests");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "InventoryAssignments");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "IdempotentRequests");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "IdempotentRequests");

            migrationBuilder.DropColumn(
                name: "LockExpiresAt",
                table: "IdempotentRequests");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "IdempotentRequests");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotentRequests_CreatedAt",
                table: "IdempotentRequests",
                column: "CreatedAt");
        }
    }
}
