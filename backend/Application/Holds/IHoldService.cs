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

    /// <summary>Pay the admission fee: HELD → CONFIRMED. Idempotent.</summary>
    /// <exception cref="Common.Exceptions.NotFoundException">The hold does not exist.</exception>
    /// <exception cref="Domain.Exceptions.DomainForbiddenException">The hold belongs to another student.</exception>
    /// <exception cref="Domain.Exceptions.DomainConflictException">The hold is expired or cancelled.</exception>
    Task<HoldDto> ConfirmHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default);

    /// <summary>Withdraw the application: HELD → CANCELLED. Idempotent.</summary>
    /// <exception cref="Common.Exceptions.NotFoundException">The hold does not exist.</exception>
    /// <exception cref="Domain.Exceptions.DomainForbiddenException">The hold belongs to another student.</exception>
    /// <exception cref="Domain.Exceptions.DomainConflictException">The hold is expired or already confirmed.</exception>
    Task CancelHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default);

    /// <summary>All applications of a student (any status), newest first.</summary>
    Task<IReadOnlyList<HoldDto>> GetStudentHoldsAsync(string studentId, CancellationToken cancellationToken = default);
}
