using Chaetsere.Domain;
using Chaetsere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chaetsere.Infrastructure.Persistence.Configurations;

public class SalonClientConfiguration : IEntityTypeConfiguration<SalonClient>
{
    public void Configure(EntityTypeBuilder<SalonClient> b)
    {
        b.ToTable("salon_clients", t => t.HasCheckConstraint("ck_salon_clients_phone", $"phone {Checks.Phone}"));
        b.Property(x => x.FullName).HasMaxLength(120);
        b.Property(x => x.Phone).HasMaxLength(16);

        b.HasAlternateKey(x => new { x.Id, x.SalonId });
        b.HasIndex(x => new { x.SalonId, x.Phone }).IsUnique();
        b.HasIndex(x => x.UserId).HasFilter("user_id IS NOT NULL");

        b.HasOne(x => x.Salon).WithMany(s => s.Clients).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class FavoriteSalonConfiguration : IEntityTypeConfiguration<FavoriteSalon>
{
    public void Configure(EntityTypeBuilder<FavoriteSalon> b)
    {
        b.ToTable("favorite_salons");
        b.HasKey(x => new { x.UserId, x.SalonId });
        b.HasOne(x => x.User).WithMany(u => u.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Salon).WithMany().HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        // The double-booking guard (EXCLUDE USING gist) is not expressible in EF —
        // it is added in ManualMigrationSql. Keep its WHERE clause in sync with BookingStatuses.OccupyTime.
        b.ToTable("bookings", t =>
        {
            t.HasCheckConstraint("ck_bookings_source", Checks.In<BookingSource>("source"));
            t.HasCheckConstraint("ck_bookings_status", Checks.In<BookingStatus>("status"));
            t.HasCheckConstraint("ck_bookings_cancelled_by", Checks.In<CancelledBy>("cancelled_by"));
            t.HasCheckConstraint("ck_bookings_range", "ends_at > starts_at");
            t.HasCheckConstraint("ck_bookings_buffer", "occupied_until >= ends_at");
            t.HasCheckConstraint("ck_bookings_price", "total_price >= 0");
            t.HasCheckConstraint("ck_bookings_cancel_timestamp", "(status IN ('cancelled', 'declined')) = (cancelled_at IS NOT NULL)");
            t.HasCheckConstraint("ck_bookings_cancel_actor", "status <> 'cancelled' OR cancelled_by IS NOT NULL");
        });

        b.Property(x => x.ReferenceCode).HasMaxLength(16);
        b.Property(x => x.Source).HasSnakeCaseEnum(8);
        b.Property(x => x.Status).HasSnakeCaseEnum();
        b.Property(x => x.CancelledBy).HasSnakeCaseEnum(8);
        b.Property(x => x.TotalPrice).HasPrecision(10, 2);
        b.Property(x => x.ClientComment).HasMaxLength(500);
        b.Property(x => x.CancelReason).HasMaxLength(200);

        b.HasIndex(x => x.ReferenceCode).IsUnique();
        b.HasIndex(x => new { x.SalonId, x.StartsAt });                                     // salon calendar
        b.HasIndex(x => new { x.SalonClientId, x.StartsAt }).IsDescending(false, true);    // client history
        b.HasIndex(x => new { x.CreatedByUserId, x.StartsAt }).IsDescending(false, true)
            .HasFilter("created_by_user_id IS NOT NULL");                                   // "my bookings" in the app
        b.HasIndex(x => x.StartsAt).HasFilter("status = 'confirmed' AND reminder_sent_at IS NULL")
            .HasDatabaseName("ix_bookings_reminder_due");                                   // SMS reminder job

        b.HasOne(x => x.Salon).WithMany().HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Restrict);
        // staff and client must belong to the booking's salon
        b.HasOne(x => x.Staff).WithMany()
            .HasForeignKey(x => new { x.StaffId, x.SalonId })
            .HasPrincipalKey(s => new { s.Id, s.SalonId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Client).WithMany(c => c.Bookings)
            .HasForeignKey(x => new { x.SalonClientId, x.SalonId })
            .HasPrincipalKey(c => new { c.Id, c.SalonId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> b)
    {
        b.ToTable("booking_services", t =>
        {
            t.HasCheckConstraint("ck_booking_services_position", "position >= 0");
            t.HasCheckConstraint("ck_booking_services_duration", "duration_minutes > 0");
            t.HasCheckConstraint("ck_booking_services_price", "price >= 0");
        });
        b.HasKey(x => new { x.BookingId, x.Position });
        b.Property(x => x.ServiceName).HasMaxLength(120);
        b.Property(x => x.Price).HasPrecision(10, 2);
        b.HasOne(x => x.Booking).WithMany(bk => bk.Services).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        // services are archived, never deleted — Restrict makes an accidental delete fail loudly
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookingEventConfiguration : IEntityTypeConfiguration<BookingEvent>
{
    public void Configure(EntityTypeBuilder<BookingEvent> b)
    {
        b.ToTable("booking_events");
        b.Property(x => x.Id).UseIdentityAlwaysColumn();
        b.Property(x => x.Type).HasMaxLength(32);
        b.Property(x => x.Payload).HasColumnType("jsonb");
        b.HasIndex(x => new { x.BookingId, x.CreatedAt });
        b.HasOne(x => x.Booking).WithMany(bk => bk.Events).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.Property(x => x.Type).HasMaxLength(32);
        b.Property(x => x.Title).HasMaxLength(160);
        b.Property(x => x.Body).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => x.UserId).HasFilter("read_at IS NULL").HasDatabaseName("ix_notifications_unread");
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Salon).WithMany().HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
{
    public void Configure(EntityTypeBuilder<SmsMessage> b)
    {
        b.ToTable("sms_messages", t =>
        {
            t.HasCheckConstraint("ck_sms_messages_purpose", Checks.In<SmsPurpose>("purpose"));
            t.HasCheckConstraint("ck_sms_messages_status", Checks.In<SmsStatus>("status"));
        });
        b.Property(x => x.Phone).HasMaxLength(16);
        b.Property(x => x.Body).HasMaxLength(640);
        b.Property(x => x.Purpose).HasSnakeCaseEnum();
        b.Property(x => x.Status).HasSnakeCaseEnum(8);
        b.Property(x => x.ProviderMessageId).HasMaxLength(64);
        b.Property(x => x.Error).HasMaxLength(500);
        b.HasIndex(x => x.CreatedAt).HasFilter("status = 'queued'").HasDatabaseName("ix_sms_messages_queue");
        b.HasIndex(x => new { x.Phone, x.CreatedAt }).IsDescending(false, true);
        b.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.SetNull);
    }
}
