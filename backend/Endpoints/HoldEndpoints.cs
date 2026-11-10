using SchoolAdmission.Endpoints.Filters;

namespace SchoolAdmission.Endpoints;

public static class HoldEndpoints
{
    public static void MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/classes/{classId:guid}/holds", HoldHandlers.CreateHold)
            .RequireStudentId()
            .WithTags("Holds");

        var group = app.MapGroup("/holds").WithTags("Holds");

        group.MapGet("/{holdId:guid}", HoldHandlers.GetHoldById);
    }
}
