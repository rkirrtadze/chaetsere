using System.Net;

namespace Chaetsere.Domain.Entities;

/// <summary>
/// Anyone who signs in with a phone number: clients, masters, owners.
/// What a user can do in a salon is decided by <see cref="StaffMember"/> rows, not by a role on the user.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Phone { get; set; }               // E.164: +995599123456
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public List<StaffMember> StaffMemberships { get; set; } = [];
    public List<FavoriteSalon> Favorites { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<PushToken> PushTokens { get; set; } = [];
}

/// <summary>One-time SMS login code. Only the hash is stored.</summary>
public class OtpCode
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Phone { get; set; }
    public required string CodeHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public short Attempts { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public IPAddress? RequestIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public string? DeviceName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }

    public User User { get; set; } = null!;
    public RefreshToken? ReplacedBy { get; set; }
}

/// <summary>Expo push token, one row per device.</summary>
public class PushToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public DevicePlatform Platform { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
}
