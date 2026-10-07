namespace Chaetsere.Domain;

// Stored in the database as snake_case text (e.g. NoShow -> "no_show"),
// guarded by CHECK constraints generated from these enums.

public enum SalonStatus { Draft, PendingReview, Published, Suspended }

public enum StaffRole { Owner, Manager, Master }

public enum StaffStatus { Invited, Active, Archived }

public enum BookingStatus { Pending, Confirmed, Completed, NoShow, Cancelled, Declined }

/// <summary>App = the client booked in the mobile app; Salon = staff added it by hand (phone call, walk-in).</summary>
public enum BookingSource { App, Salon }

public enum CancelledBy { Client, Salon }

public enum DevicePlatform { Ios, Android }

public enum SmsPurpose { Otp, Confirmation, Reminder, Rescheduled, Cancelled, Invite }

public enum SmsStatus { Queued, Sent, Failed }

public static class BookingStatuses
{
    /// <summary>Statuses that hold the master's time. Must match the WHERE clause of the overlap guard.</summary>
    public static readonly BookingStatus[] OccupyTime =
        [BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.Completed, BookingStatus.NoShow];
}

public static class BookingEventTypes
{
    public const string Created = "created";
    public const string Confirmed = "confirmed";
    public const string Declined = "declined";
    public const string Moved = "moved";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
    public const string NoShow = "no_show";
    public const string StatusReverted = "status_reverted";
}
