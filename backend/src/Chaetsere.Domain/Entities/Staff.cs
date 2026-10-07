namespace Chaetsere.Domain.Entities;

/// <summary>
/// A person who works in a salon: owner, manager or master.
/// Exists before they ever sign in (status Invited, UserId null); linked to a <see cref="User"/> when they accept.
/// </summary>
public class StaffMember
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SalonId { get; set; }
    public Guid? UserId { get; set; }
    public required string DisplayName { get; set; }
    public required string Phone { get; set; }
    public StaffRole Role { get; set; }
    public bool IsBookable { get; set; } = true;            // appears as a calendar column and in the app
    public string? Specialization { get; set; }
    public string? AvatarUrl { get; set; }
    public short ColorIndex { get; set; }
    public StaffStatus Status { get; set; } = StaffStatus.Invited;
    public int SortOrder { get; set; }
    public DateTimeOffset InvitedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? JoinedAt { get; set; }

    public Salon Salon { get; set; } = null!;
    public User? User { get; set; }
    public List<StaffService> Services { get; set; } = [];
    public List<StaffWorkingHours> WorkingHours { get; set; } = [];
    public List<StaffTimeOff> TimeOff { get; set; } = [];
}

/// <summary>Which master performs which service. SalonId is repeated so the database can enforce "same salon".</summary>
public class StaffService
{
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid SalonId { get; set; }

    public StaffMember Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>Weekly working template in the salon's local time. No row for a weekday = day off.</summary>
public class StaffWorkingHours
{
    public Guid StaffId { get; set; }
    public DayOfWeek Weekday { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public TimeOnly? BreakStartsAt { get; set; }
    public TimeOnly? BreakEndsAt { get; set; }

    public StaffMember Staff { get; set; } = null!;
}

/// <summary>Blocked time: personal errand, doctor, vacation. Clients can't book it.</summary>
public class StaffTimeOff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid StaffId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? Reason { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public StaffMember Staff { get; set; } = null!;
    public User? CreatedBy { get; set; }
}
