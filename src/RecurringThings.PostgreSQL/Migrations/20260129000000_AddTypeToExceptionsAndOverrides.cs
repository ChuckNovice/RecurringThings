namespace RecurringThings.PostgreSQL.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Adds the Type column to occurrence_exceptions and occurrence_overrides tables.
/// The Type is denormalized from the parent recurrence for indexing efficiency.
/// </summary>
public partial class AddTypeToExceptionsAndOverrides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "type",
            table: "occurrence_overrides",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "type",
            table: "occurrence_exceptions",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "type",
            table: "occurrence_overrides");

        migrationBuilder.DropColumn(
            name: "type",
            table: "occurrence_exceptions");
    }
}
