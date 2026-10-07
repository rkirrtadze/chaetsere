using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chaetsere.Infrastructure.Persistence;

public static class DbErrors
{
    /// <summary>
    /// True when SaveChanges failed because the master already has a booking at that time
    /// (ex_bookings_no_overlap, SQLSTATE 23P01). Return 409 "ეს დრო უკვე დაკავებულია".
    /// </summary>
    public static bool IsBookingOverlap(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: "ex_bookings_no_overlap" };

    public static bool IsUniqueViolation(this DbUpdateException ex, string? constraintName = null) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (constraintName is null || pg.ConstraintName == constraintName);
}
