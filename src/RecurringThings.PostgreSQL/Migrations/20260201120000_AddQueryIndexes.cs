#nullable disable

namespace RecurringThings.PostgreSQL.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Adds indexes to optimize common query patterns for event retrieval.
/// </summary>
/// <remarks>
/// <para>
/// This migration adds the following indexes:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>ix_events_tenant_user_enddate</term>
///     <description>Primary index for user-specific queries with date filtering</description>
///   </item>
///   <item>
///     <term>ix_events_tenant_user_uid</term>
///     <description>Point lookup index for Update and Delete operations</description>
///   </item>
///   <item>
///     <term>ix_events_tenant_enddate</term>
///     <description>Tenant-wide index for admin/reporting queries (UserMode.All)</description>
///   </item>
///   <item>
///     <term>ix_event_categories_value</term>
///     <description>Category value index for InCategories() filtering</description>
///   </item>
/// </list>
/// </remarks>
public partial class AddQueryIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Primary index for user-specific queries (UserMode.Specific and UserMode.TenantWide)
        // Supports: TenantId + UserId + date range filtering
        migrationBuilder.CreateIndex(
            name: "ix_events_tenant_user_enddate",
            table: "events",
            columns: ["TenantId", "UserId", "EndDate"]);

        // Point lookup index for Update and Delete operations
        // Supports: TenantId + UserId + Uid exact match
        migrationBuilder.CreateIndex(
            name: "ix_events_tenant_user_uid",
            table: "events",
            columns: ["TenantId", "UserId", "Uid"]);

        // Tenant-wide index for admin/reporting queries (UserMode.All)
        // Supports: TenantId + date range filtering without UserId
        migrationBuilder.CreateIndex(
            name: "ix_events_tenant_enddate",
            table: "events",
            columns: ["TenantId", "EndDate"]);

        // Category value index for InCategories() filtering
        // Supports: WHERE value IN (...) subqueries
        migrationBuilder.CreateIndex(
            name: "ix_event_categories_value",
            table: "event_categories",
            column: "Value");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_events_tenant_user_enddate",
            table: "events");

        migrationBuilder.DropIndex(
            name: "ix_events_tenant_user_uid",
            table: "events");

        migrationBuilder.DropIndex(
            name: "ix_events_tenant_enddate",
            table: "events");

        migrationBuilder.DropIndex(
            name: "ix_event_categories_value",
            table: "event_categories");
    }
}
