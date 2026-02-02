#nullable disable

namespace RecurringThings.PostgreSQL.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                Uid = table.Column<string>(type: "text", nullable: false),
                ComponentType = table.Column<int>(type: "integer", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<string>(type: "text", nullable: true),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                SerializedData = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_events", x => x.Id));

        migrationBuilder.CreateTable(
            name: "event_categories",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                EventId = table.Column<long>(type: "bigint", nullable: false),
                Value = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_categories", x => x.Id);
                table.ForeignKey(
                    name: "FK_event_categories_events_EventId",
                    column: x => x.EventId,
                    principalTable: "events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "event_properties",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                EventId = table.Column<long>(type: "bigint", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_properties", x => x.Id);
                table.ForeignKey(
                    name: "FK_event_properties_events_EventId",
                    column: x => x.EventId,
                    principalTable: "events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_event_categories_EventId",
            table: "event_categories",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_event_properties_EventId",
            table: "event_properties",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_events_Uid",
            table: "events",
            column: "Uid",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "event_categories");

        migrationBuilder.DropTable(
            name: "event_properties");

        migrationBuilder.DropTable(
            name: "events");
    }
}
