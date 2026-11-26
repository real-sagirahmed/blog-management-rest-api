using System.Text.Json;
using BlogManagement.API.Exceptions;
using BlogManagement.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlogManagement.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_LeavesResponseUntouched()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Theory]
    [MemberData(nameof(ApiExceptions))]
    public async Task InvokeAsync_WhenApiExceptionThrown_ReturnsProblemDetails(
        Exception exception,
        int expectedStatus,
        string expectedTitle,
        string expectedDetail)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/posts/99";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problem);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
        Assert.Equal("/api/posts/99", problem.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_Returns500WithoutLeakingDetails()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/posts";
        context.Response.Body = new MemoryStream();

        const string sensitiveDetail = "SqlConnection failure: Server=prod-db;Password=SuperSecret123;";
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException(sensitiveDetail),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("SuperSecret123", body);
        Assert.DoesNotContain("SqlConnection", body);

        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
    }

    public static TheoryData<Exception, int, string, string> ApiExceptions() => new()
    {
        { new BadRequestException("CategoryId 99 does not exist."), StatusCodes.Status400BadRequest, "Bad Request", "CategoryId 99 does not exist." },
        { new NotFoundException("Post 99 was not found."), StatusCodes.Status404NotFound, "Not Found", "Post 99 was not found." },
        { new ConflictException("Category already exists."), StatusCodes.Status409Conflict, "Conflict", "Category already exists." }
    };
}
