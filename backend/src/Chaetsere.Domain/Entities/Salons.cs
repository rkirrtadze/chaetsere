namespace Chaetsere.Domain.Entities;

public class Salon
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public int? DistrictId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CoverImageUrl { get; set; }
    public string TimeZone { get; set; } = "Asia/Tbilisi";
    public SalonStatus Status { get; set; } = SalonStatus.Draft;
    public bool IsListed { get; set; } = true;               // owner's "visible in the app" toggle
    public decimal? RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    public District? District { get; set; }
    public User CreatedBy { get; set; } = null!;
    public SalonSettings Settings { get; set; } = null!;
    public List<SalonPhoto> Photos { get; set; } = [];
    public List<SalonWorkingHours> WorkingHours { get; set; } = [];
    public List<SalonClosure> Closures { get; set; } = [];
    public List<Service> Services { get; set; } = [];
    public List<StaffMember> Staff { get; set; } = [];
    public List<SalonClient> Clients { get; set; } = [];
}

/// <summary>1:1 with <see cref="Salon"/> — the "booking rules" screen.</summary>
public class SalonSettings
{
    public Guid SalonId { get; set; }
    public bool AutoConfirm { get; set; } = true;
    public int MinLeadMinutes { get; set; } = 60;
    public int MaxAdvanceDays { get; set; } = 30;
    public int ClientCancelCutoffMinutes { get; set; } = 120;
    public short SlotStepMinutes { get; set; } = 30;
    public bool SmsRemindersEnabled { get; set; } = true;
    public int ReminderMinutesBefore { get; set; } = 120;

    public Salon Salon { get; set; } = null!;
}

public class SalonPhoto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SalonId { get; set; }
    public required string Url { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Salon Salon { get; set; } = null!;
}

/// <summary>Weekly opening hours in the salon's local time. No row for a weekday = closed.</summary>
public class SalonWorkingHours
{
    public Guid SalonId { get; set; }
    public DayOfWeek Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    public Salon Salon { get; set; } = null!;
}

/// <summary>Holiday or one-off closed day.</summary>
public class SalonClosure
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SalonId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }

    public Salon Salon { get; set; } = null!;
}
