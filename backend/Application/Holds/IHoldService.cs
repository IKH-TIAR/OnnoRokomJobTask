using SchoolAdmission.Application.Holds.Dtos;

namespace SchoolAdmission.Application.Holds;

public interface IHoldService
{
    /// <summary>Apply for a seat: reserves one seat for the student for the hold duration.</summary>
    /// <exception cref="Common.Exceptions.NotFoundException">The class does not exist.</exception>
    /// <exception cref="Domain.Exceptions.DomainConflictException">Class is full or the student already applied.</exception>
    Task<HoldDto> CreateHoldAsync(Guid classId, string studentId, CancellationToken cancellationToken = default);

    /// <exception cref="Common.Exceptions.NotFoundException">The hold does not exist.</exception>
    Task<HoldDto> GetByIdAsync(Guid holdId, CancellationToken cancellationToken = default);
}
