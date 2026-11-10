using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Classes.Dtos;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Infrastructure.Persistence.Services;

public class ClassService(AppDbContext dbContext, TimeProvider timeProvider) : IClassService
{
    public async Task<ClassDto> CreateClassAsync(CreateClassDto dto, CancellationToken cancellationToken = default)
    {
        var schoolClass = SchoolClass.Create(dto.Name!, dto.TotalSeats!.Value, timeProvider.GetUtcNow());

        await dbContext.Classes.AddAsync(schoolClass, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClassDto(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats);
    }

    public async Task<IReadOnlyList<ClassSeatSummaryDto>> GetAllClassesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await SeatCounts(dbContext.Classes.OrderBy(c => c.CreatedAt))
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<ClassSeatSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await SeatCounts(dbContext.Classes.Where(c => c.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("CLASS_NOT_FOUND", "The requested class does not exist.");

        return ToDto(row);
    }

    /// <summary>
    /// Seat counts computed in SQL. A HELD hold only occupies a seat until it expires, so
    /// seats return to the pool the moment a hold's time runs out, even before the
    /// background sweeper marks it EXPIRED.
    /// </summary>
    private IQueryable<SeatCountRow> SeatCounts(IQueryable<SchoolClass> classes)
    {
        var now = timeProvider.GetUtcNow();

        return classes
            .AsNoTracking()
            .Select(c => new SeatCountRow(
                c.Id,
                c.Name,
                c.TotalSeats,
                dbContext.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Held && h.ExpiresAt > now),
                dbContext.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Confirmed)));
    }

    private static ClassSeatSummaryDto ToDto(SeatCountRow r) => new(
        r.Id,
        r.Name,
        r.TotalSeats,
        r.HeldSeats,
        r.ConfirmedSeats,
        Math.Max(0, r.TotalSeats - r.HeldSeats - r.ConfirmedSeats));

    private sealed record SeatCountRow(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats);
}
