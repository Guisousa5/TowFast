using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TowFast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "base_price",
                table: "service_types",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_per_km",
                table: "service_types",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_distance_km",
                table: "service_requests",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_price",
                table: "service_requests",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

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
                columns: new[] { "base_price", "price_per_km" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "base_price", "price_per_km" },
                values: new object[] { null, null });

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_types_base_price",
                table: "service_types",
                sql: "base_price IS NULL OR base_price > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_types_price_per_km",
                table: "service_types",
                sql: "price_per_km IS NULL OR price_per_km > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_distance",
                table: "service_requests",
                sql: "estimated_distance_km IS NULL OR estimated_distance_km >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_quoted_price",
                table: "service_requests",
                sql: "quoted_price IS NULL OR quoted_price > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_service_types_base_price",
                table: "service_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_types_price_per_km",
                table: "service_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_distance",
                table: "service_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_quoted_price",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "base_price",
                table: "service_types");

            migrationBuilder.DropColumn(
                name: "price_per_km",
                table: "service_types");

            migrationBuilder.DropColumn(
                name: "estimated_distance_km",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "quoted_price",
                table: "service_requests");
        }
    }
}
