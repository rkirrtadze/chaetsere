using Chaetsere.Domain;
using Chaetsere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chaetsere.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("services", t =>
        {
            t.HasCheckConstraint("ck_services_duration", "duration_minutes BETWEEN 5 AND 720");
            t.HasCheckConstraint("ck_services_buffer", "buffer_minutes BETWEEN 0 AND 120");
            t.HasCheckConstraint("ck_services_price", "price >= 0");
        });
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Price).HasPrecision(10, 2);

        // (id, salon_id) is the target of same-salon composite foreign keys
        b.HasAlternateKey(x => new { x.Id, x.SalonId });
        b.HasIndex(x => new { x.SalonId, x.SortOrder }).HasFilter("NOT is_archived");

        b.HasOne(x => x.Salon).WithMany(s => s.Services).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> b)
    {
        b.ToTable("staff_members", t =>
        {
            t.HasCheckConstraint("ck_staff_members_phone", $"phone {Checks.Phone}");
            t.HasCheckConstraint("ck_staff_members_role", Checks.In<StaffRole>("role"));
            t.HasCheckConstraint("ck_staff_members_status", Checks.In<StaffStatus>("status"));
        });
        b.Property(x => x.DisplayName).HasMaxLength(120);
        b.Property(x => x.Phone).HasMaxLength(16);
        b.Property(x => x.Specialization).HasMaxLength(120);
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
        b.Property(x => x.Role).HasSnakeCaseEnum();
        b.Property(x => x.Status).HasSnakeCaseEnum();

        b.HasAlternateKey(x => new { x.Id, x.SalonId });
        b.HasIndex(x => new { x.SalonId, x.Phone }).IsUnique();
        b.HasIndex(x => new { x.SalonId, x.UserId }).IsUnique().HasFilter("user_id IS NOT NULL");

        b.HasOne(x => x.Salon).WithMany(s => s.Staff).HasForeignKey(x => x.SalonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany(u => u.StaffMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class StaffServiceConfiguration : IEntityTypeConfiguration<StaffService>
{
    public void Configure(EntityTypeBuilder<StaffService> b)
    {
        b.ToTable("staff_services");
        b.HasKey(x => new { x.StaffId, x.ServiceId });
        b.HasIndex(x => x.ServiceId);

        // both FKs include salon_id, so a master can only get services of their own salon
        b.HasOne(x => x.Staff).WithMany(s => s.Services)
            .HasForeignKey(x => new { x.StaffId, x.SalonId })
            .HasPrincipalKey(s => new { s.Id, s.SalonId })
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Service).WithMany(s => s.StaffServices)
            .HasForeignKey(x => new { x.ServiceId, x.SalonId })
            .HasPrincipalKey(s => new { s.Id, s.SalonId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffWorkingHoursConfiguration : IEntityTypeConfiguration<StaffWorkingHours>
{
    public void Configure(EntityTypeBuilder<StaffWorkingHours> b)
    {
        b.ToTable("staff_working_hours", t =>
        {
            t.HasCheckConstraint("ck_staff_working_hours_weekday", "weekday BETWEEN 0 AND 6");
            t.HasCheckConstraint("ck_staff_working_hours_range", "starts_at < ends_at");
            t.HasCheckConstraint("ck_staff_working_hours_break_pair", "(break_starts_at IS NULL) = (break_ends_at IS NULL)");
            t.HasCheckConstraint("ck_staff_working_hours_break_inside",
                "break_starts_at IS NULL OR (break_starts_at >= starts_at AND break_ends_at <= ends_at AND break_starts_at < break_ends_at)");
        });
        b.HasKey(x => new { x.StaffId, x.Weekday });
        b.Property(x => x.Weekday).HasWeekdayColumn();
        b.HasOne(x => x.Staff).WithMany(s => s.WorkingHours).HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffTimeOffConfiguration : IEntityTypeConfiguration<StaffTimeOff>
{
    public void Configure(EntityTypeBuilder<StaffTimeOff> b)
    {
        // the GiST range index on (staff_id, tstzrange(starts_at, ends_at)) is added in ManualMigrationSql
        b.ToTable("staff_time_off", t => t.HasCheckConstraint("ck_staff_time_off_range", "starts_at < ends_at"));
        b.Property(x => x.Reason).HasMaxLength(120);
        b.HasOne(x => x.Staff).WithMany(s => s.TimeOff).HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
