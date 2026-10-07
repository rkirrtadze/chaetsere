namespace Chaetsere.Domain.Entities;

/// <summary>In-app feed (the bell icon) for clients and staff.</summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid? SalonId { get; set; }
    public Guid? BookingId { get; set; }
    public required string Type { get; set; }                // booking_created, booking_cancelled, reminder …
    public required string Title { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Salon? Salon { get; set; }
    public Booking? Booking { get; set; }
}

/// <summary>SMS outbox and log. Hangfire picks up Queued rows; the log also shows what SMS costs you.</summary>
public class SmsMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Phone { get; set; }
    public required string Body { get; set; }
    public SmsPurpose Purpose { get; set; }
    public Guid? BookingId { get; set; }
    public SmsStatus Status { get; set; } = SmsStatus.Queued;
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }

    public Booking? Booking { get; set; }
}
