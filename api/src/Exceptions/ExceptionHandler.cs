using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Boardly.Api.Exceptions;

public class ExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
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
                ForbidenException => StatusCodes.Status403Forbidden,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                _ => StatusCodes.Status500InternalServerError
            },
            Detail = exception.Message
        };
        if (problemDetails.Status == StatusCodes.Status501NotImplemented)
            problemDetails.Detail = null;
        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
            return false;

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            Exception = exception,
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}