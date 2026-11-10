using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Application.Holds.Dtos;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using SchoolAdmission.Infrastructure.Persistence.Configurations;

namespace SchoolAdmission.Infrastructure.Persistence.Services;

/// <remarks>
/// Concurrency: every operation that changes a hold first locks the class row
/// (<see cref="LockClassAsync"/>), then the hold row. All writers for a class are therefore
/// serialized, which guarantees held + confirmed never exceeds total seats, even with several
/// API processes on the same database. The fixed lock order (class → hold) prevents deadlocks.
/// </remarks>
public class HoldService(AppDbContext dbContext, TimeProvider timeProvider, HoldSettings holdSettings) : IHoldService
{
    public async Task<HoldDto> CreateHoldAsync(Guid classId, string studentId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var schoolClass = await LockClassAsync(classId, cancellationToken);

        // Persist expiry for holds whose time has run out, so their seats are free and
        // the student may apply again (the unique index only covers HELD/CONFIRMED rows).
        await dbContext.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Expired)
                .SetProperty(h => h.UpdatedAt, now), cancellationToken);

        var activeHolds = dbContext.Holds
            .Where(h => h.ClassId == classId && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (await activeHolds.AnyAsync(h => h.StudentId == studentId, cancellationToken))
            throw AlreadyApplied();

        var occupiedSeats = await activeHolds.CountAsync(cancellationToken);

        var hold = schoolClass.PlaceHold(studentId, occupiedSeats, now, holdSettings.Duration);

        dbContext.Holds.Add(hold);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: HoldConfiguration.ActiveStudentPerClassIndex
        })
        {
            throw AlreadyApplied();
        }

        await transaction.CommitAsync(cancellationToken);

        return ToDto(hold, schoolClass.Name, now);
    }

    public async Task<HoldDto> GetByIdAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var result = await HoldsWithClassName(h => h.Id == holdId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw HoldNotFound();

        return ToDto(result.Hold, result.ClassName, timeProvider.GetUtcNow());
    }

    public async Task<HoldDto> ConfirmHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var (hold, schoolClass) = await LockOwnedHoldAsync(holdId, studentId, cancellationToken);

        hold.Confirm(now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDto(hold, schoolClass.Name, now);
    }

    public async Task CancelHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var (hold, _) = await LockOwnedHoldAsync(holdId, studentId, cancellationToken);

        hold.Cancel(now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HoldDto>> GetStudentHoldsAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var results = await HoldsWithClassName(h => h.StudentId == studentId)
            .ToListAsync(cancellationToken);

        return results.Select(x => ToDto(x.Hold, x.ClassName, now)).ToList();
    }

    /// <summary>Row-locks the class until the current transaction ends.</summary>
    private async Task<SchoolClass> LockClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        // ToListAsync (not FirstOrDefault) so EF does not wrap the raw SQL in a subquery.
        var rows = await dbContext.Classes
            .FromSql($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault()
            ?? throw new NotFoundException("CLASS_NOT_FOUND", "The requested class does not exist.");
    }

    /// <summary>
    /// Verifies ownership, then locks the class and the hold (in that order) and returns
    /// the freshly re-read, tracked hold.
    /// </summary>
    private async Task<(Hold Hold, SchoolClass SchoolClass)> LockOwnedHoldAsync(
        Guid holdId, string studentId, CancellationToken cancellationToken)
    {
        // ClassId and StudentId never change, so they can be read before locking.
        var info = await dbContext.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new { h.ClassId, h.StudentId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw HoldNotFound();

        if (info.StudentId != studentId)
            throw new DomainForbiddenException("HOLD_NOT_OWNED", "This application belongs to a different student.");

        var schoolClass = await LockClassAsync(info.ClassId, cancellationToken);

        var hold = (await dbContext.Holds
                .FromSql($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
                .ToListAsync(cancellationToken))
            .Single();

        hold.EnsureOwnedBy(studentId);

        return (hold, schoolClass);
    }

    /// <summary>
    /// Holds matching <paramref name="filter"/> joined with their class name, newest first.
    /// Filtering/ordering happen on the entity before projection so EF can translate them.
    /// </summary>
    private IQueryable<HoldWithClassName> HoldsWithClassName(Expression<Func<Hold, bool>> filter) =>
        from h in dbContext.Holds.AsNoTracking().Where(filter)
        join c in dbContext.Classes on h.ClassId equals c.Id
        orderby h.CreatedAt descending
        select new HoldWithClassName(h, c.Name);

    private static HoldDto ToDto(Hold hold, string className, DateTimeOffset now) => new(
        hold.Id,
        hold.ClassId,
        className,
        hold.StudentId,
        hold.GetEffectiveStatus(now),
        hold.ExpiresAt.UtcDateTime);

    private static NotFoundException HoldNotFound() =>
        new("HOLD_NOT_FOUND", "The requested application does not exist.");

    private static DomainConflictException AlreadyApplied() => new(
        "ALREADY_APPLIED",
        "This student already has an active application or admission for this class.");

    private sealed record HoldWithClassName(Hold Hold, string ClassName);
}
