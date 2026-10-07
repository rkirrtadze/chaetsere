using Microsoft.EntityFrameworkCore.Migrations;

namespace Chaetsere.Infrastructure.Persistence;

/// <summary>
/// Database objects EF Core can't model. Call from the Initial migration:
/// <code>
/// protected override void Up(MigrationBuilder mb)   { … generated code …; mb.AddManualSql(); }
/// protected override void Down(MigrationBuilder mb) { mb.DropManualSql(); … generated code … }
/// </code>
/// </summary>
public static class ManualMigrationSql
{
    // Keep the status list in sync with BookingStatuses.OccupyTime.
    private const string Up = """
        ALTER TABLE bookings ADD CONSTRAINT ex_bookings_no_overlap EXCLUDE USING gist (
            staff_id WITH =,
            tstzrange(starts_at, occupied_until) WITH &&
        ) WHERE (status IN ('pending', 'confirmed', 'completed', 'no_show'));

        CREATE INDEX ix_staff_time_off_range ON staff_time_off
            USING gist (staff_id, tstzrange(starts_at, ends_at));
        """;

    private const string Down = """
        DROP INDEX IF EXISTS ix_staff_time_off_range;
        ALTER TABLE bookings DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;
        """;

    public static void AddManualSql(this MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Up);

    public static void DropManualSql(this MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Down);
}
