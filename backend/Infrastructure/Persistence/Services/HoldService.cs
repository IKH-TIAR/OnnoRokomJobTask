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

public class HoldService(AppDbContext dbContext, TimeProvider timeProvider, HoldSettings holdSettings) : IHoldService
{
    public async Task<HoldDto> CreateHoldAsync(Guid classId, string studentId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Lock the class row. Every concurrent hold request for this class (from any API
        // process sharing this database) waits here until we commit, so "count seats then
        // insert" below cannot interleave and overbook the class.
        // ToListAsync (not FirstOrDefault) so EF does not wrap the raw SQL in a subquery.
        var schoolClass = (await dbContext.Classes
                .FromSql($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                .ToListAsync(cancellationToken))
            .SingleOrDefault()
            ?? throw new NotFoundException("CLASS_NOT_FOUND", "The requested class does not exist.");

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
        var result = await (
                from h in dbContext.Holds.AsNoTracking()
                join c in dbContext.Classes on h.ClassId equals c.Id
                where h.Id == holdId
                select new { Hold = h, ClassName = c.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("HOLD_NOT_FOUND", "The requested application does not exist.");

        return ToDto(result.Hold, result.ClassName, timeProvider.GetUtcNow());
    }

    private static HoldDto ToDto(Hold hold, string className, DateTimeOffset now) => new(
        hold.Id,
        hold.ClassId,
        className,
        hold.StudentId,
        hold.GetEffectiveStatus(now),
        hold.ExpiresAt.UtcDateTime);

    private static DomainConflictException AlreadyApplied() => new(
        "ALREADY_APPLIED",
        "This student already has an active application or admission for this class.");
}
