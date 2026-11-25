using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TowFast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureServicePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "base_price", "price_per_km" },
                values: new object[] { 100m, 3m });

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "base_price",
                value: 150m);

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "base_price",
                value: 80m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "base_price", "price_per_km" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "base_price",
                value: null);

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "base_price",
                value: null);
        }
    }
}
