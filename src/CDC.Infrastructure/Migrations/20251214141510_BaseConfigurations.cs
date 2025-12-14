using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BaseConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cdc_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "text", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: false),
                    operation = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    partition_key = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cdc_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routing_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: false),
                    exchange = table.Column<string>(type: "text", nullable: false),
                    routing_key = table.Column<string>(type: "text", nullable: false),
                    queue = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_configurations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cdc_events_message_id",
                table: "cdc_events",
                column: "message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cdc_events_status",
                table: "cdc_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_routing_configurations_is_active",
                table: "routing_configurations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_routing_configurations_table_name",
                table: "routing_configurations",
                column: "table_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cdc_events");

            migrationBuilder.DropTable(
                name: "routing_configurations");
        }
    }
}
