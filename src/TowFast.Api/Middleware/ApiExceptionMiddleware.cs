using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TowFast.Application.Common;
using TowFast.Domain.Common;

namespace TowFast.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var status = exception switch
            {
                ValidationException or DomainException => StatusCodes.Status400BadRequest,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException or DbUpdateConcurrencyException or DbUpdateException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            if (status >= 500) logger.LogError(exception, "Unhandled API error. CorrelationId={CorrelationId}", context.TraceIdentifier);
            else logger.LogWarning("API request rejected. Status={Status} Type={Type} CorrelationId={CorrelationId}", status, exception.GetType().Name, context.TraceIdentifier);

            var detail = exception switch
            {
                DbUpdateConcurrencyException => "The data changed during the operation. Refresh and try again.",
                DbUpdateException => "The operation conflicts with the current database state.",
                _ when status == 500 => "An unexpected error occurred.",
                _ => exception.Message
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.com/{status}"
            };
            problem.Extensions["correlationId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
