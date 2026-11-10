using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Domain.Entities;

namespace SchoolAdmission.Api.Infrastructure.Persistence.Configurations;

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("classes", t =>
        {
            t.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0");
        });

        // Names are pinned so the model matches the names created by the initial
        // migration; the snake-case convention would otherwise rename them to
        // pk_classes/fk_holds_classes_class_id and produce a bogus pending migration.
        builder.HasKey(c => c.Id).HasName("PK_classes");
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(c => c.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(c => c.Holds)
            .WithOne(h => h.Class)
            .HasForeignKey(h => h.ClassId)
            .HasConstraintName("FK_holds_classes_class_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
