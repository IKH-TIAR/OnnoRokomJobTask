using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using SchoolAdmission.Domain.ValueObjects;

namespace SchoolAdmission.Domain.Entities;

/// <summary>
/// A student's application for a seat in a class. The seat is reserved until
/// <see cref="ExpiresAt"/>; if the admission fee is not paid by then, the hold
/// expires and the seat returns to the pool.
/// </summary>
public class Hold
{
    // For EF Core
    private Hold() { }

    public Guid Id { get; private set; }
    public Guid ClassId { get; private set; }
    public string StudentId { get; private set; } = null!;
    public HoldStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Only <see cref="SchoolClass.PlaceHold"/> creates holds, so the seat-capacity
    /// rule cannot be bypassed.
    /// </summary>
    internal static Hold Create(Guid classId, string studentId, DateTimeOffset now, TimeSpan duration)
    {
        if (!ValueObjects.StudentId.IsValid(studentId))
            throw new DomainException("STUDENT_ID_INVALID",
                $"Student ID must be a {ValueObjects.StudentId.Length}-digit birth registration number.");
        if (duration <= TimeSpan.Zero)
            throw new DomainException("HOLD_DURATION_INVALID", "Hold duration must be positive.");

        now = TruncateToMicroseconds(now);

        return new Hold
        {
            Id = Guid.CreateVersion7(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now + duration
        };
    }

    // PostgreSQL stores timestamps with microsecond precision; truncating up front makes the
    // value returned on create identical to the value read back later.
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        => new(value.Ticks - value.Ticks % 10, value.Offset);

    /// <summary>
    /// The status as the outside world should see it. A hold whose time ran out is
    /// EXPIRED even if the background sweeper has not persisted that yet.
    /// </summary>
    public HoldStatus GetEffectiveStatus(DateTimeOffset now)
        => Status == HoldStatus.Held && now >= ExpiresAt ? HoldStatus.Expired : Status;
}
