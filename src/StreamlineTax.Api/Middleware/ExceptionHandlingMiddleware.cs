using System.Net;
using System.Text.Json;
using StreamlineTax.Application.Common.Exceptions;

namespace StreamlineTax.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception {CorrelationId} {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                NotFoundException => (int)HttpStatusCode.NotFound,
                FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
                ValidationException => (int)HttpStatusCode.BadRequest,
                TaxPeriodLockedException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var errorCode = ex switch
            {
                StreamlineTax.Application.Common.Exceptions.ApplicationException appEx => appEx.ErrorCode,
                KeyNotFoundException => "NOT_FOUND",
                FluentValidation.ValidationException => "VALIDATION_ERROR",
                _ => "INTERNAL_ERROR"
            };

            var result = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                errorCode,
                correlationId
            });
            await context.Response.WriteAsync(result);
        }
    }
}
