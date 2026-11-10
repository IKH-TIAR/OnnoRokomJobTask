using FluentValidation;

namespace SchoolAdmission.Endpoints.Filters;

public class ValidationFilter<T>(IValidator<T>? validator = null) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (validator is null)
        {
            return await next(context);
        }

        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Request body cannot be empty.",
                extensions: new Dictionary<string, object?> { ["code"] = "BAD_REQUEST" });
        }

        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.ToDictionary(),
                extensions: new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED" });
        }

        return await next(context);
    }
}
