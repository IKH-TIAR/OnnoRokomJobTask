using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

/// <summary>
/// An admission unit with its own seat pool. Every shift of a grade is a separate
/// SchoolClass, e.g. "Class 6 - Morning Shift" and "Class 6 - Day Shift".
/// </summary>
public class SchoolClass
{
    public const int NameMaxLength = 200;
    public const int MaxSeats = 10_000;

    // For EF Core
    private SchoolClass() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int TotalSeats { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static SchoolClass Create(string name, int totalSeats, DateTimeOffset now)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            throw new DomainException("CLASS_NAME_REQUIRED", "Class name is required.");
        if (trimmed.Length > NameMaxLength)
            throw new DomainException("CLASS_NAME_TOO_LONG", $"Class name must be at most {NameMaxLength} characters.");
        if (totalSeats is < 1 or > MaxSeats)
            throw new DomainException("CLASS_INVALID_SEATS", $"Total seats must be between 1 and {MaxSeats}.");

        return new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = trimmed,
            TotalSeats = totalSeats,
            CreatedAt = now
        };
    }
}
