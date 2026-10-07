using System.Text.Json;

namespace Chaetsere.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string ReferenceCode { get; set; }       // CH-1042, from booking_reference_seq
    public Guid SalonId { get; set; }
    public Guid StaffId { get; set; }
    public Guid SalonClientId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public BookingSource Source { get; set; }
    public BookingStatus Status { get; set; }

    /// <summary>UTC. Npgsql only writes DateTimeOffset with offset 0 — call ToUniversalTime() first.</summary>
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }               // end of the last service
    public DateTimeOffset OccupiedUntil { get; set; }        // EndsAt + cleanup buffer; used by the overlap guard

    public decimal TotalPrice { get; set; }
    public string? ClientComment { get; set; }
    public string? CancelReason { get; set; }
    public CancelledBy? CancelledBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ReminderSentAt { get; set; }

    public Salon Salon { get; set; } = null!;
    public StaffMember Staff { get; set; } = null!;
    public SalonClient Client { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public List<BookingService> Services { get; set; } = [];
    public List<BookingEvent> Events { get; set; } = [];
}

/// <summary>Snapshot of each booked service, so later price or name edits don't rewrite history.</summary>
public class BookingService
{
    public Guid BookingId { get; set; }
    public short Position { get; set; }
    public Guid ServiceId { get; set; }
    public required string ServiceName { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }

    public Booking Booking { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>Audit trail: who created, confirmed, moved or cancelled a booking, and when.</summary>
public class BookingEvent
{
    public long Id { get; set; }
    public Guid BookingId { get; set; }
    public required string Type { get; set; }                // see BookingEventTypes
    public Guid? ActorUserId { get; set; }
    public JsonDocument? Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Booking Booking { get; set; } = null!;
    public User? Actor { get; set; }
}
