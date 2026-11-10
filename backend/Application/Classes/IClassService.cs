using SchoolAdmission.Application.Classes.Dtos;

namespace SchoolAdmission.Application.Classes;

public interface IClassService
{
    Task<ClassDto> CreateClassAsync(CreateClassDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassSeatSummaryDto>> GetAllClassesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="Common.Exceptions.NotFoundException">The class does not exist.</exception>
    Task<ClassSeatSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
