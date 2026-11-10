using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Classes.Dtos;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Entities;

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
        var classes = await dbContext.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Name, c.TotalSeats })
            .ToListAsync(cancellationToken);

        return classes.Select(c => ToSeatSummary(c.Id, c.Name, c.TotalSeats)).ToList();
    }

    public async Task<ClassSeatSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Id, c.Name, c.TotalSeats })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("CLASS_NOT_FOUND", "The requested class does not exist.");

        return ToSeatSummary(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats);
    }

    // Holds are not implemented yet. Once they are, held/confirmed will be counted from the
    // holds table (active hold = HELD and expires_at in the future).
    private static ClassSeatSummaryDto ToSeatSummary(Guid id, string name, int totalSeats)
    {
        const int held = 0, confirmed = 0;
        return new ClassSeatSummaryDto(id, name, totalSeats, held, confirmed, totalSeats - held - confirmed);
    }
}
