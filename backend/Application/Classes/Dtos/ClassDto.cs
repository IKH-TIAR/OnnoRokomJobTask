namespace SchoolAdmission.Application.Classes.Dtos;

/// <summary>201 response of POST /classes. Contract: { id, name, totalSeats }.</summary>
public record ClassDto(
    Guid Id,
    string Name,
    int TotalSeats
);
