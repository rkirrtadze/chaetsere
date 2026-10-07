namespace Chaetsere.Domain.Entities;

/// <summary>
/// The salon's own client card. Exists for app users and for people booked by phone who have no account.
/// When an app user books, find the card by (SalonId, Phone) or create it, and set UserId.
/// The note (allergies, preferences) is private to this salon.
/// </summary>
public class SalonClient
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SalonId { get; set; }
    public Guid? UserId { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Salon Salon { get; set; } = null!;
    public User? User { get; set; }
    public List<Booking> Bookings { get; set; } = [];
}

public class FavoriteSalon
{
    public Guid UserId { get; set; }
    public Guid SalonId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Salon Salon { get; set; } = null!;
}
