using SchoolAdmission.Domain.ValueObjects;

namespace SchoolAdmission.Endpoints.Filters;

/// <summary>
/// Requires a valid X-Student-Id header (17-digit birth registration number)
/// before the handler runs.
/// </summary>
public class StudentIdFilter : IEndpointFilter
{
    public const string HeaderName = "X-Student-Id";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var value = context.HttpContext.Request.Headers[HeaderName].ToString().Trim();

        if (value.Length == 0)
        {
            return Problem("STUDENT_ID_REQUIRED",
                $"Please enter the student's birth registration number ({HeaderName} header is missing).");
        }

        if (!StudentId.IsValid(value))
        {
            return Problem("STUDENT_ID_INVALID",
                $"Birth registration number must be exactly {StudentId.Length} digits.");
        }

        return await next(context);
    }

    private static IResult Problem(string code, string detail) => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Bad Request",
        detail: detail,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}

public static class StudentIdExtension
{
    public static RouteHandlerBuilder RequireStudentId(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<StudentIdFilter>();
}
