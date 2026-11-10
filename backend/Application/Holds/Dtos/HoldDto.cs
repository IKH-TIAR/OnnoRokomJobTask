using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Holds.Dtos;

/// <summary>
/// Response of POST /classes/{classId}/holds and GET /holds/{holdId}.
/// Contract fields: holdId, classId, studentId, status, expiresAt.
/// className is an extra field so the UI can show it without another request.
/// </summary>
public record HoldDto(
    Guid HoldId,
    Guid ClassId,
    string ClassName,
    string StudentId,
    HoldStatus Status,
    DateTime ExpiresAt // DateTime (UTC) so it serializes with a "Z" suffix, like the contract
);
