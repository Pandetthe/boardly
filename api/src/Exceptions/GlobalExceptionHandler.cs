using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Boardly.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = exception switch
            {
                RecordDoesNotExist => StatusCodes.Status404NotFound,
                RecordAlreadyExists => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                PreconditionFailedException => StatusCodes.Status412PreconditionFailed,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                _ => StatusCodes.Status500InternalServerError
            },
            Detail = exception.Message
        };
        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Detail = null;
            _logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }
        else if (problemDetails.Status == StatusCodes.Status501NotImplemented)
        {
            problemDetails.Detail = null;
            _logger.LogWarning("Endpoint not implemented: {Message}", exception.Message);
        }
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            Exception = exception,
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}