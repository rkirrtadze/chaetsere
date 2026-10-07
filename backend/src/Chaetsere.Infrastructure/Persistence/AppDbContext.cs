using Chaetsere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chaetsere.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // reference data
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();
    public DbSet<District> Districts => Set<District>();

    // users & auth
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();

    // salons
    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<SalonSettings> SalonSettings => Set<SalonSettings>();
    public DbSet<SalonPhoto> SalonPhotos => Set<SalonPhoto>();
    public DbSet<SalonWorkingHours> SalonWorkingHours => Set<SalonWorkingHours>();
    public DbSet<SalonClosure> SalonClosures => Set<SalonClosure>();

    // catalog & staff
    public DbSet<Service> Services => Set<Service>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffService> StaffServices => Set<StaffService>();
    public DbSet<StaffWorkingHours> StaffWorkingHours => Set<StaffWorkingHours>();
    public DbSet<StaffTimeOff> StaffTimeOff => Set<StaffTimeOff>();

    // clients
    public DbSet<SalonClient> SalonClients => Set<SalonClient>();
    public DbSet<FavoriteSalon> FavoriteSalons => Set<FavoriteSalon>();

    // bookings
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<BookingEvent> BookingEvents => Set<BookingEvent>();

    // notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.HasSequence<long>("booking_reference_seq").StartsAt(1000);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>Next human-readable booking code, e.g. "CH-1042".</summary>
    public async Task<string> NextBookingReferenceAsync(CancellationToken ct = default)
    {
        var next = await Database
            .SqlQuery<long>($"SELECT nextval('booking_reference_seq') AS \"Value\"")
            .SingleAsync(ct);
        return $"CH-{next}";
    }
}
