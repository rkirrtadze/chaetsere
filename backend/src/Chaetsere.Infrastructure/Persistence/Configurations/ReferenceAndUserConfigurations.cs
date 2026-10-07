using Chaetsere.Domain;
using Chaetsere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chaetsere.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories");
        b.Property(x => x.Code).HasMaxLength(32);
        b.Property(x => x.Name).HasMaxLength(64);
        b.Property(x => x.Icon).HasMaxLength(32);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class ServiceTemplateConfiguration : IEntityTypeConfiguration<ServiceTemplate>
{
    public void Configure(EntityTypeBuilder<ServiceTemplate> b)
    {
        b.ToTable("service_templates", t =>
        {
            t.HasCheckConstraint("ck_service_templates_duration", "duration_minutes BETWEEN 5 AND 720");
            t.HasCheckConstraint("ck_service_templates_buffer", "buffer_minutes BETWEEN 0 AND 120");
            t.HasCheckConstraint("ck_service_templates_price", "default_price >= 0");
        });
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.DefaultPrice).HasPrecision(10, 2);
        b.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
        b.HasOne(x => x.Category).WithMany(c => c.Templates).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> b)
    {
        b.ToTable("districts");
        b.Property(x => x.City).HasMaxLength(64);
        b.Property(x => x.Name).HasMaxLength(64);
        b.HasIndex(x => new { x.City, x.Name }).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users", t => t.HasCheckConstraint("ck_users_phone", $"phone {Checks.Phone}"));
        b.Property(x => x.Phone).HasMaxLength(16);
        b.Property(x => x.FullName).HasMaxLength(120);
        b.Property(x => x.Email).HasMaxLength(254);
        b.HasIndex(x => x.Phone).IsUnique();
    }
}

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> b)
    {
        b.ToTable("otp_codes", t => t.HasCheckConstraint("ck_otp_codes_attempts", "attempts >= 0"));
        b.Property(x => x.Phone).HasMaxLength(16);
        b.Property(x => x.CodeHash).HasMaxLength(128);
        b.HasIndex(x => new { x.Phone, x.CreatedAt }).IsDescending(false, true);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.TokenHash).HasMaxLength(128);
        b.Property(x => x.DeviceName).HasMaxLength(120);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReplacedBy).WithMany().HasForeignKey(x => x.ReplacedById).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
{
    public void Configure(EntityTypeBuilder<PushToken> b)
    {
        b.ToTable("push_tokens", t => t.HasCheckConstraint("ck_push_tokens_platform", Checks.In<DevicePlatform>("platform")));
        b.Property(x => x.Token).HasMaxLength(255);
        b.Property(x => x.Platform).HasSnakeCaseEnum(8);
        b.HasIndex(x => x.Token).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.PushTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
