namespace RecurringThings.PostgreSQL.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "occurrences",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                extensions = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_occurrences", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "recurrences",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                recurrence_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                r_rule = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                extensions = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recurrences", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "occurrence_exceptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                recurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                original_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                extensions = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_occurrence_exceptions", x => x.id);
                table.ForeignKey(
                    name: "FK_occurrence_exceptions_recurrences_recurrence_id",
                    column: x => x.recurrence_id,
                    principalTable: "recurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "occurrence_overrides",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                recurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                original_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                original_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                original_extensions = table.Column<string>(type: "jsonb", nullable: true),
                extensions = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_occurrence_overrides", x => x.id);
                table.ForeignKey(
                    name: "FK_occurrence_overrides_recurrences_recurrence_id",
                    column: x => x.recurrence_id,
                    principalTable: "recurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "idx_exceptions_query",
            table: "occurrence_exceptions",
            columns: new[] { "organization", "resource_path", "original_time_utc" });

        migrationBuilder.CreateIndex(
            name: "idx_exceptions_recurrence",
            table: "occurrence_exceptions",
            column: "recurrence_id");

        migrationBuilder.CreateIndex(
            name: "idx_overrides_original",
            table: "occurrence_overrides",
            columns: new[] { "organization", "resource_path", "original_time_utc" });

        migrationBuilder.CreateIndex(
            name: "idx_overrides_recurrence",
            table: "occurrence_overrides",
            column: "recurrence_id");

        migrationBuilder.CreateIndex(
            name: "idx_overrides_start",
            table: "occurrence_overrides",
            columns: new[] { "organization", "resource_path", "start_time", "end_time" });

        migrationBuilder.CreateIndex(
            name: "idx_occurrences_query",
            table: "occurrences",
            columns: new[] { "organization", "resource_path", "type", "start_time", "end_time" });

        migrationBuilder.CreateIndex(
            name: "idx_recurrences_query",
            table: "recurrences",
            columns: new[] { "organization", "resource_path", "type", "start_time", "recurrence_end_time" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "occurrence_exceptions");

        migrationBuilder.DropTable(
            name: "occurrence_overrides");

        migrationBuilder.DropTable(
            name: "occurrences");

        migrationBuilder.DropTable(
            name: "recurrences");
    }
}
