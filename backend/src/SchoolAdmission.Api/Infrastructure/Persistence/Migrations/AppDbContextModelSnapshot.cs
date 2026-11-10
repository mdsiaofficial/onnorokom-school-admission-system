using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SchoolAdmission.Api.Infrastructure.Persistence;

#nullable disable

namespace SchoolAdmission.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("SchoolAdmission.Api.Domain.Entities.Class", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(120)
                    .HasColumnType("character varying(120)")
                    .HasColumnName("name");

                b.Property<int>("TotalSeats")
                    .HasColumnType("integer")
                    .HasColumnName("total_seats");

                b.HasKey("Id");

                b.ToTable("classes", null, t =>
                    {
                        t.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0");
                    });
            });

        modelBuilder.Entity("SchoolAdmission.Api.Domain.Entities.Hold", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTimeOffset?>("CancelledAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("cancelled_at");

                b.Property<Guid>("ClassId")
                    .HasColumnType("uuid")
                    .HasColumnName("class_id");

                b.Property<DateTimeOffset?>("ConfirmedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("confirmed_at");

                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                b.Property<DateTimeOffset>("ExpiresAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("expires_at");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("status");

                b.Property<string>("StudentId")
                    .IsRequired()
                    .HasMaxLength(17)
                    .HasColumnType("character(17)")
                    .IsFixedLength()
                    .HasColumnName("student_id");

                b.Property<DateTimeOffset>("UpdatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at");

                b.HasKey("Id");

                b.HasIndex("ClassId", "Status", "ExpiresAt")
                    .HasDatabaseName("ix_holds_class_status_expires");

                b.HasIndex("StudentId", "CreatedAt")
                    .HasDatabaseName("ix_holds_student_created");

                b.HasIndex("StudentId")
                    .IsUnique()
                    .HasDatabaseName("ux_holds_active_student")
                    .HasFilter("status IN ('HELD', 'CONFIRMED')");

                b.ToTable("holds", null, t =>
                    {
                        t.HasCheckConstraint("ck_holds_status_valid", "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                        t.HasCheckConstraint("ck_holds_student_id_17_digits", "student_id ~ '^[0-9]{17}$'");
                    });
            });

        modelBuilder.Entity("SchoolAdmission.Api.Domain.Entities.Hold", b =>
            {
                b.HasOne("SchoolAdmission.Api.Domain.Entities.Class", "Class")
                    .WithMany("Holds")
                    .HasForeignKey("ClassId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("Class");
            });

        modelBuilder.Entity("SchoolAdmission.Api.Domain.Entities.Class", b =>
            {
                b.Navigation("Holds");
            });
#pragma warning restore 612, 618
    }
}
