using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes", t =>
            t.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(SchoolClass.NameMaxLength);

        builder.Property(c => c.TotalSeats)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
