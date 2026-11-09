using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Domain.Entities;
using SchoolAdmission.Api.Domain.Enums;

namespace SchoolAdmission.Api.Infrastructure.Persistence.Configurations;

public class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("holds", t =>
        {
            t.HasCheckConstraint("ck_holds_student_id_17_digits", "student_id ~ '^[0-9]{17}$'");
            t.HasCheckConstraint("ck_holds_status_valid", "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property(h => h.ClassId)
            .HasColumnName("class_id")
            .IsRequired();

        builder.Property(h => h.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(17)
            .IsFixedLength()
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<HoldStatus>(v))
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(h => h.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(h => h.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .IsRequired(false);

        builder.Property(h => h.CancelledAt)
            .HasColumnName("cancelled_at")
            .IsRequired(false);

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_holds_class_status_expires");

        builder.HasIndex(h => new { h.StudentId, h.CreatedAt })
            .HasDatabaseName("ix_holds_student_created");

        builder.HasIndex(h => h.StudentId)
            .HasDatabaseName("ux_holds_active_student")
            .HasFilter("status IN ('HELD', 'CONFIRMED')")
            .IsUnique();
    }
}
