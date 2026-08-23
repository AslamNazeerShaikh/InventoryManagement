using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryUniqueWhenPresentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enforces per-tenant "unique when present" barcode/serial numbers, which the
            // application-level pre-check alone cannot guarantee under concurrency. Rows with no
            // barcode/serial and soft-deleted rows are exempt. NOTE: on an existing database this
            // fails if duplicates were already admitted by the previous (non-unique) indexes; those
            // rows must be reconciled first.
            migrationBuilder.CreateIndex(
                name: "UX_Inventories_TenantId_Barcode",
                table: "Inventories",
                columns: new[] { "TenantId", "Barcode" },
                unique: true,
                filter: "\"Barcode\" IS NOT NULL AND \"Barcode\" <> '' AND \"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Inventories_TenantId_SerialNumber",
                table: "Inventories",
                columns: new[] { "TenantId", "SerialNumber" },
                unique: true,
                filter: "\"SerialNumber\" IS NOT NULL AND \"SerialNumber\" <> '' AND \"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Inventories_TenantId_Barcode",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "UX_Inventories_TenantId_SerialNumber",
                table: "Inventories");
        }
    }
}
