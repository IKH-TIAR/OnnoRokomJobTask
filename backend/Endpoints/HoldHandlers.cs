using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Endpoints.Filters;

namespace SchoolAdmission.Endpoints;

// studentId parameters are validated by StudentIdFilter (.RequireStudentId()). They are nullable
// so a missing header reaches the filter (friendly message) instead of failing model binding.
public static class HoldHandlers
{
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

    public static async Task<IResult> ConfirmHold(
        Guid holdId,
        [FromHeader(Name = StudentIdFilter.HeaderName)] string? studentId,
        IHoldService holdService,
        CancellationToken ct)
    {
        var hold = await holdService.ConfirmHoldAsync(holdId, studentId!.Trim(), ct);
        return Results.Ok(hold);
    }

    public static async Task<IResult> CancelHold(
        Guid holdId,
        [FromHeader(Name = StudentIdFilter.HeaderName)] string? studentId,
        IHoldService holdService,
        CancellationToken ct)
    {
        await holdService.CancelHoldAsync(holdId, studentId!.Trim(), ct);
        return Results.NoContent();
    }

    public static async Task<IResult> GetMyHolds(
        [FromHeader(Name = StudentIdFilter.HeaderName)] string? studentId,
        IHoldService holdService,
        CancellationToken ct)
    {
        var holds = await holdService.GetStudentHoldsAsync(studentId!.Trim(), ct);
        return Results.Ok(holds);
    }
}
