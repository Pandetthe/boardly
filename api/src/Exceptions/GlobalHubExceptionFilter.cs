using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Boardly.Api.Exceptions;

public class GlobalHubExceptionFilter : IHubFilter
{
    private readonly ILogger<GlobalHubExceptionFilter> _logger;

    public GlobalHubExceptionFilter(ILogger<GlobalHubExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception exception)
        {
            var status = exception switch
            {
                RecordDoesNotExist => 404,
                RecordAlreadyExists => 400,
                ArgumentException => 400,
                UnauthorizedException => 401,
                ForbiddenException => 403,
                PreconditionFailedException => 412,
                NotImplementedException => 501,
                _ => 500
            };

            if (status == 500)
            {
                _logger.LogError(exception, "An unhandled exception occurred in SignalR hub method: {Method}", invocationContext.HubMethodName);
                var problemDetails = new
                {
                    Status = status,
                    Title = "Internal Server Error",
                };
                throw new HubException(JsonSerializer.Serialize(problemDetails));
            }
            else if (status == 501)
            {
                _logger.LogWarning("SignalR hub method not implemented: {Method} - {Message}", invocationContext.HubMethodName, exception.Message);
                var problemDetails = new
                {
                    Status = status,
                    Title = "Not Implemented",
                };
                throw new HubException(JsonSerializer.Serialize(problemDetails));
            }
            else
            {
                return new
                {
                    Success = false,
                    Status = status,
                    Error = exception.Message
                };
            }
        }
    }
}
