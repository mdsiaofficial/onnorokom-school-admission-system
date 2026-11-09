using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolAdmission.Api.Infrastructure.Persistence;

#nullable disable

namespace SchoolAdmission.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20261109000001_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "classes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                total_seats = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_classes", x => x.id);
                table.CheckConstraint("ck_classes_total_seats_positive", "total_seats > 0");
            });

        migrationBuilder.CreateTable(
            name: "holds",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                class_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<string>(type: "character(17)", fixedLength: true, maxLength: 17, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_holds", x => x.id);
                table.CheckConstraint("ck_holds_status_valid", "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                table.CheckConstraint("ck_holds_student_id_17_digits", "student_id ~ '^[0-9]{17}$'");
                table.ForeignKey(
                    name: "FK_holds_classes_class_id",
                    column: x => x.class_id,
                    principalTable: "classes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_holds_class_status_expires",
            table: "holds",
            columns: new[] { "class_id", "status", "expires_at" });

        migrationBuilder.CreateIndex(
            name: "ix_holds_student_created",
            table: "holds",
            columns: new[] { "student_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ux_holds_active_student_class",
            table: "holds",
            columns: new[] { "student_id", "class_id" },
            unique: true,
            filter: "status IN ('HELD', 'CONFIRMED')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "holds");

        migrationBuilder.DropTable(
            name: "classes");
    }
}
