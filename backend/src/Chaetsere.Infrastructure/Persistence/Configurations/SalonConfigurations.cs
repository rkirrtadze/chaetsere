using Chaetsere.Domain;
using Chaetsere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chaetsere.Infrastructure.Persistence.Configurations;

public class SalonConfiguration : IEntityTypeConfiguration<Salon>
{
    public void Configure(EntityTypeBuilder<Salon> b)
    {
        b.ToTable("salons", t =>
        {
            t.HasCheckConstraint("ck_salons_status", Checks.In<SalonStatus>("status"));
            t.HasCheckConstraint("ck_salons_latitude", "latitude BETWEEN -90 AND 90");
            t.HasCheckConstraint("ck_salons_longitude", "longitude BETWEEN -180 AND 180");
            t.HasCheckConstraint("ck_salons_coordinates_pair", "(latitude IS NULL) = (longitude IS NULL)");
            t.HasCheckConstraint("ck_salons_rating", "rating_avg BETWEEN 1 AND 5");
            // a salon can only go live with an address and a pin on the map
            t.HasCheckConstraint("ck_salons_publishable",
                "status NOT IN ('pending_review', 'published') OR (address IS NOT NULL AND latitude IS NOT NULL)");
        });

        b.Property(x => x.Slug).HasMaxLength(80);
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Phone).HasMaxLength(16);
        b.Property(x => x.Address).HasMaxLength(200);
        b.Property(x => x.CoverImageUrl).HasMaxLength(500);
        b.Property(x => x.TimeZone).HasMaxLength(64);
        b.Property(x => x.Status).HasSnakeCaseEnum();
        b.Property(x => x.RatingAvg).HasPrecision(2, 1);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.DistrictId).HasFilter("status = 'published' AND is_listed").HasDatabaseName("ix_salons_listing");

        b.HasOne(x => x.District).WithMany().HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Settings).WithOne(s => s.Salon).HasForeignKey<SalonSettings>(s => s.SalonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalonSettingsConfiguration : IEntityTypeConfiguration<SalonSettings>
{
    public void Configure(EntityTypeBuilder<SalonSettings> b)
    {
        b.ToTable("salon_settings", t =>
        {
            t.HasCheckConstraint("ck_salon_settings_lead", "min_lead_minutes BETWEEN 0 AND 10080");
            t.HasCheckConstraint("ck_salon_settings_advance", "max_advance_days BETWEEN 1 AND 365");
            t.HasCheckConstraint("ck_salon_settings_cancel", "client_cancel_cutoff_minutes >= 0");
            t.HasCheckConstraint("ck_salon_settings_step", "slot_step_minutes IN (5, 10, 15, 20, 30, 60)");
            t.HasCheckConstraint("ck_salon_settings_reminder", "reminder_minutes_before BETWEEN 15 AND 2880");
        });
        b.HasKey(x => x.SalonId);
    }
}

public class SalonPhotoConfiguration : IEntityTypeConfiguration<SalonPhoto>
{
    public void Configure(EntityTypeBuilder<SalonPhoto> b)
    {
        b.ToTable("salon_photos");
        b.Property(x => x.Url).HasMaxLength(500);
        b.HasIndex(x => new { x.SalonId, x.SortOrder });
        b.HasOne(x => x.Salon).WithMany(s => s.Photos).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalonWorkingHoursConfiguration : IEntityTypeConfiguration<SalonWorkingHours>
{
    public void Configure(EntityTypeBuilder<SalonWorkingHours> b)
    {
        b.ToTable("salon_working_hours", t =>
        {
            t.HasCheckConstraint("ck_salon_working_hours_weekday", "weekday BETWEEN 0 AND 6");
            t.HasCheckConstraint("ck_salon_working_hours_range", "opens_at < closes_at");
        });
        b.HasKey(x => new { x.SalonId, x.Weekday });
        b.Property(x => x.Weekday).HasWeekdayColumn();
        b.HasOne(x => x.Salon).WithMany(s => s.WorkingHours).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalonClosureConfiguration : IEntityTypeConfiguration<SalonClosure>
{
    public void Configure(EntityTypeBuilder<SalonClosure> b)
    {
        b.ToTable("salon_closures");
        b.Property(x => x.Reason).HasMaxLength(200);
        b.HasIndex(x => new { x.SalonId, x.Date }).IsUnique();
        b.HasOne(x => x.Salon).WithMany(s => s.Closures).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
    }
}
