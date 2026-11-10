namespace SchoolAdmission.Domain.ValueObjects;

/// <summary>
/// A student is identified by their 17-digit birth registration number.
/// </summary>
public static class StudentId
{
    public const int Length = 17;

    public static bool IsValid(string? value)
        => value is { Length: Length } && value.All(char.IsAsciiDigit);
}
