using SchoolAdmission.Application.Classes.Dtos;
using SchoolAdmission.Endpoints.Filters;

namespace SchoolAdmission.Endpoints;

public static class ClassEndpoints
{
    public static void MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/classes").WithTags("Classes");

        group.MapPost("/", ClassHandlers.CreateClass)
            .WithValidation<CreateClassDto>();

        group.MapGet("/", ClassHandlers.GetAllClasses);

        group.MapGet("/{classId:guid}", ClassHandlers.GetClassById);
    }
}
