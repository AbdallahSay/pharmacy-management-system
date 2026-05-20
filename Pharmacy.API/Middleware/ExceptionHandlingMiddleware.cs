using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace Pharmacy.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var resolvedException = Unwrap(exception);

        var (statusCode, title) = resolvedException switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            DbUpdateException dbEx when IsForeignKeyViolation(dbEx) => (
                HttpStatusCode.Conflict,
                "Conflict"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        object body = resolvedException switch
        {
            ValidationException validationEx => new
            {
                title,
                status = (int)statusCode,
                errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            },
            DbUpdateException dbEx when IsForeignKeyViolation(dbEx) => new
            {
                title,
                status = (int)statusCode,
                detail = "Cannot delete this medicine because it is referenced by existing sales."
            },
            _ => new
            {
                title,
                status = (int)statusCode,
                detail = resolvedException.Message
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static Exception Unwrap(Exception exception)
    {
        return exception switch
        {
            DbUpdateException => exception,
            _ when exception.InnerException is not null => Unwrap(exception.InnerException),
            _ => exception
        };
    }

    private static bool IsForeignKeyViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 547 })
                return true;
        }

        return false;
    }
}
