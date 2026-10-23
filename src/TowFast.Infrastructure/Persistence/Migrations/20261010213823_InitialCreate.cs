using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TowFast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    document = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "text", nullable: false),
                    trade_name = table.Column<string>(type: "text", nullable: true),
                    document = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: false),
                    service_center_latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    service_center_longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    service_radius_km = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.id);
                    table.CheckConstraint("ck_providers_radius", "service_radius_km > 0");
                });

            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    requires_destination = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    manufacturer = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    model_year = table.Column<int>(type: "integer", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_vehicles_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    label = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "text", nullable: true),
                    complement = table.Column<string>(type: "text", nullable: true),
                    district = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    reference = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_saved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                    table.CheckConstraint("ck_locations_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_locations_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_locations_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    document = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    license_number = table.Column<string>(type: "text", nullable: false),
                    license_category = table.Column<string>(type: "text", nullable: false),
                    license_expires_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_drivers_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "text", nullable: false),
                    vehicle_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    capacity_kg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_vehicles_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_service_types",
                columns: table => new
                {
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_service_types", x => new { x.provider_id, x.service_type_id });
                    table.ForeignKey(
                        name: "FK_provider_service_types_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_provider_service_types_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_replaced_by_token_id",
                        column: x => x.replaced_by_token_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_notes = table.Column<string>(type: "text", nullable: true),
                    operator_notes = table.Column<string>(type: "text", nullable: true),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    en_route_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_requests_client_vehicles_client_vehicle_id",
                        column: x => x.client_vehicle_id,
                        principalTable: "client_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_locations_destination_location_id",
                        column: x => x.destination_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_locations_origin_location_id",
                        column: x => x.origin_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    release_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_assignments_service_requests_service_reques~",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_request_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_items", x => x.id);
                    table.CheckConstraint("ck_request_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_service_request_items_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_request_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_status_history_service_requests_service_req~",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_request_tracking",
                columns: table => new
                {
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    accuracy_meters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    speed_kmh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    heading_degrees = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_tracking", x => x.service_request_id);
                    table.ForeignKey(
                        name: "FK_service_request_tracking_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_client_vehicles_client_id",
                table: "client_vehicles",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_vehicles_license_plate",
                table: "client_vehicles",
                column: "license_plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_document",
                table: "clients",
                column: "document",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_email",
                table: "clients",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_document",
                table: "drivers",
                column: "document",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_license_number",
                table: "drivers",
                column: "license_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_provider_id",
                table: "drivers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_locations_client_id",
                table: "locations",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_service_types_service_type_id",
                table: "provider_service_types",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_vehicles_license_plate",
                table: "provider_vehicles",
                column: "license_plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_vehicles_provider_id",
                table: "provider_vehicles",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_providers_document",
                table: "providers",
                column: "document",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_replaced_by_token_id",
                table: "refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_assignments_service_request_id_accepted_at",
                table: "service_request_assignments",
                columns: new[] { "service_request_id", "accepted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_items_service_request_id",
                table: "service_request_items",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_status_history_service_request_id_created_at",
                table: "service_request_status_history",
                columns: new[] { "service_request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_request_tracking_source_event_id",
                table: "service_request_tracking",
                column: "source_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_client_id",
                table: "service_requests",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_client_vehicle_id",
                table: "service_requests",
                column: "client_vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_destination_location_id",
                table: "service_requests",
                column: "destination_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_origin_location_id",
                table: "service_requests",
                column: "origin_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_provider_id",
                table: "service_requests",
                column: "provider_id",
                unique: true,
                filter: "provider_id IS NOT NULL AND status IN ('Accepted', 'EnRoute', 'InProgress')");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_public_id",
                table: "service_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_service_type_id",
                table: "service_requests",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_status_created_at_public_id",
                table: "service_requests",
                columns: new[] { "status", "created_at", "public_id" });

            migrationBuilder.CreateIndex(
                name: "IX_service_types_code",
                table: "service_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_client_id",
                table: "users",
                column: "client_id",
                unique: true,
                filter: "client_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_driver_id",
                table: "users",
                column: "driver_id",
                unique: true,
                filter: "driver_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drivers");

            migrationBuilder.DropTable(
                name: "provider_service_types");

            migrationBuilder.DropTable(
                name: "provider_vehicles");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "service_request_assignments");

            migrationBuilder.DropTable(
                name: "service_request_items");

            migrationBuilder.DropTable(
                name: "service_request_status_history");

            migrationBuilder.DropTable(
                name: "service_request_tracking");

            migrationBuilder.DropTable(
                name: "providers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "client_vehicles");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
