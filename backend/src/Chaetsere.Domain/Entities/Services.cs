namespace Chaetsere.Domain.Entities;

/// <summary>A service a salon offers. Never hard-deleted: bookings reference it, so set <see cref="IsArchived"/>.</summary>
public class Service
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SalonId { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }                   // cleanup time after the service
    public decimal Price { get; set; }
    public bool PriceIsFrom { get; set; }                    // shown as "-დან"
    public bool IsOnline { get; set; } = true;               // bookable from the app
    public bool IsArchived { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Salon Salon { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public List<StaffService> StaffServices { get; set; } = [];
}
