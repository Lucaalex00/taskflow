using System.Net;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Exceptions;

namespace TaskFlow.Api.Middleware;

/// <summary>
/// Translates Application-layer exceptions into RFC 7807 ProblemDetails responses,
/// so controllers stay free of try/catch blocks for expected error cases.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Validation error", ex.Message, ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Resource not found", ex.Message);
        }
        catch (AuthenticationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Authentication failed", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Unexpected error",
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, HttpStatusCode statusCode, string title, string detail,
        IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
    }
}
