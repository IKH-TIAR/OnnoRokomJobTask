namespace SchoolAdmission.Application.Classes.Dtos;

/// <summary>
/// Response of GET /classes and GET /classes/{classId}.
/// Contract: { id, name, totalSeats, heldSeats, confirmedSeats, availableSeats }.
/// </summary>
public record ClassSeatSummaryDto(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats
);
