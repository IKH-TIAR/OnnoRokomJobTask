using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Infrastructure.Exceptions;

/// <summary>
/// Maps exceptions to RFC 7807 ProblemDetails with a machine-readable "code"
/// so the UI can show a human-friendly message.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, code) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Resource Not Found", e.Message, e.Code),
            DomainConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Message, e.Code),
            DomainException e => (StatusCodes.Status400BadRequest, "Bad Request", e.Message, e.Code),
            // Malformed JSON, wrong types, missing body, invalid header binding, etc.
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request",
                "The request is malformed. Please check the data you sent.", "BAD_REQUEST"),
            // Database temporarily overloaded (too many connections, pool timeout) during a rush.
            _ when IsTransientDatabaseError(exception) => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable",
                "The server is very busy right now. Please try again in a few seconds.", "SERVICE_BUSY"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "Something went wrong on our side. Please try again later.", "INTERNAL_ERROR")
        };

        if (statusCode == StatusCodes.Status503ServiceUnavailable)
        {
            httpContext.Response.Headers.RetryAfter = "3";
            logger.LogWarning(exception, "Database busy while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request {Method} {Path} failed with {StatusCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path,
                Extensions = { ["code"] = code }
            }
        });
    }

    // EF Core may wrap the Npgsql exception (e.g. DbUpdateException), so check the chain.
    private static bool IsTransientDatabaseError(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is NpgsqlException { IsTransient: true })
                return true;
        }

        return false;
    }
}
