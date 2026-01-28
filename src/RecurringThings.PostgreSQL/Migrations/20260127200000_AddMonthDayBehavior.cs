namespace RecurringThings.PostgreSQL.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddMonthDayBehavior : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "month_day_behavior",
            table: "recurrences",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "month_day_behavior",
            table: "recurrences");
    }
}
