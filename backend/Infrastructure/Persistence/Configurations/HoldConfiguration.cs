using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.ValueObjects;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public const string ActiveStudentPerClassIndex = "ux_holds_active_student_per_class";

    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("holds");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.StudentId)
            .IsRequired()
            .HasMaxLength(StudentId.Length)
            .IsFixedLength();

        // Stored as text ("Held", "Confirmed", ...) so the data is readable in the DB.
        builder.Property(h => h.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.CreatedAt).IsRequired();
        builder.Property(h => h.ExpiresAt).IsRequired();

        builder.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seat counting per class.
        builder.HasIndex(h => new { h.ClassId, h.Status });

        // "My applications" lookup.
        builder.HasIndex(h => h.StudentId);

        // Safety net for "one active application/admission per student per class":
        // even if application code had a bug, the database refuses a second active row.
        builder.HasIndex(h => new { h.ClassId, h.StudentId })
            .IsUnique()
            .HasFilter("status IN ('Held', 'Confirmed')")
            .HasDatabaseName(ActiveStudentPerClassIndex);
    }
}
