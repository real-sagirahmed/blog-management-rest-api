using System.Diagnostics;
using BlogManagement.API.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Method} {Path} was canceled by the client.", context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(exception, "Response already started; cannot write ProblemDetails.");
            return;
        }

        var (statusCode, title, detail) = exception switch
        {
            ApiException apiEx => (apiEx.StatusCode, apiEx.Title, apiEx.Message),
            DbUpdateException dbEx when IsConstraintConflict(dbEx) => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "The operation conflicted with an existing record or related data."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later."
            )
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning("Handled {StatusCode} exception on {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Type = GetRfc9110TypeUri(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    private static bool IsConstraintConflict(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sqlEx)
        {
            // 2601 / 2627: unique index / primary key violation; 547: foreign key constraint violation.
            return sqlEx.Number is 2601 or 2627 or 547;
        }

        // Support SQLite constraint errors (code 19) during in-memory integration tests without
        // taking a production package dependency on Microsoft.Data.Sqlite.
        var inner = exception.InnerException;
        if (inner?.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException")
        {
            var errorCodeObj = inner.GetType().GetProperty("SqliteErrorCode")?.GetValue(inner);
            return errorCodeObj is int errorCode && errorCode == 19;
        }

        return false;
    }

    private static string GetRfc9110TypeUri(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
