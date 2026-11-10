using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Endpoints.Filters;

namespace SchoolAdmission.Endpoints;

public static class HoldHandlers
{
    // studentId is validated by StudentIdFilter; nullable so a missing header reaches the
    // filter (which returns a friendly message) instead of failing model binding.
    public static async Task<IResult> CreateHold(
        Guid classId,
        [FromHeader(Name = StudentIdFilter.HeaderName)] string? studentId,
        IHoldService holdService,
        CancellationToken ct)
    {
        var hold = await holdService.CreateHoldAsync(classId, studentId!.Trim(), ct);
        return Results.Created($"/holds/{hold.HoldId}", hold);
    }

    public static async Task<IResult> GetHoldById(
        Guid holdId,
        IHoldService holdService,
        CancellationToken ct)
    {
        var hold = await holdService.GetByIdAsync(holdId, ct);
        return Results.Ok(hold);
    }
}
