namespace SchoolAdmission.Application.Classes.Dtos;

// Nullable so a missing field produces a "required" validation message instead of defaulting to 0.
public record CreateClassDto(
    string? Name,
    int? TotalSeats
);
