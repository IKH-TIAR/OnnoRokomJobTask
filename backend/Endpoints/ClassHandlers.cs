using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Classes.Dtos;

namespace SchoolAdmission.Endpoints;

public static class ClassHandlers
{
    public static async Task<IResult> CreateClass(
        CreateClassDto dto,
        IClassService classService,
        CancellationToken ct)
    {
        var createdClass = await classService.CreateClassAsync(dto, ct);
        return Results.Created($"/classes/{createdClass.Id}", createdClass);
    }

    public static async Task<IResult> GetAllClasses(
        IClassService classService,
        CancellationToken ct)
    {
        var classes = await classService.GetAllClassesAsync(ct);
        return Results.Ok(classes);
    }

    public static async Task<IResult> GetClassById(
        Guid classId,
        IClassService classService,
        CancellationToken ct)
    {
        var schoolClass = await classService.GetByIdAsync(classId, ct);
        return Results.Ok(schoolClass);
    }
}
