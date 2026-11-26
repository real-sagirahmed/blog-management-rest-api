using BlogManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Data;

public static class DbSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var aspNet = new Category
        {
            Name = "ASP.NET Core",
            Description = "Articles about RESTful API design and ASP.NET Core architecture.",
            CreatedAt = now.AddDays(-5)
        };

        var efCore = new Category
        {
            Name = "Entity Framework Core",
            Description = "Data modeling, migrations, and query performance optimization.",
            CreatedAt = now.AddDays(-4)
        };

        var sqlServer = new Category
        {
            Name = "SQL Server",
            Description = "Indexing, constraints, and relational database best practices.",
            CreatedAt = now.AddDays(-3)
        };

        context.Categories.AddRange(aspNet, efCore, sqlServer);
        await context.SaveChangesAsync();

        context.Posts.AddRange(
            new Post
            {
                Title = "Getting Started with ASP.NET Core 9 Web API",
                Slug = "getting-started-with-asp-net-core-9-web-api",
                Excerpt = "Build clean, maintainable REST APIs using controllers, DTOs, and ProblemDetails.",
                Content = "ASP.NET Core provides a fast, modular foundation for building production RESTful APIs with built-in dependency injection, model validation, and OpenAPI support.",
                CategoryId = aspNet.Id,
                CreatedAt = now.AddDays(-3)
            },
            new Post
            {
                Title = "Global Error Handling with RFC 9110 ProblemDetails",
                Slug = "global-error-handling-with-rfc-9110-problemdetails",
                Excerpt = "Standardize 400, 404, 409, and 500 API responses cleanly.",
                Content = "Consistent error responses improve client reliability and prevent sensitive server stack traces from leaking in production environments.",
                CategoryId = aspNet.Id,
                CreatedAt = now.AddDays(-2)
            },
            new Post
            {
                Title = "Optimizing Read Queries in EF Core with AsNoTracking and Projections",
                Slug = "optimizing-read-queries-in-ef-core-with-asnotracking-and-projections",
                Excerpt = "Reduce memory allocations and avoid over-fetching large columns.",
                Content = "Combining AsNoTracking with Select DTO projections ensures Entity Framework Core selects only the columns your API contract actually needs.",
                CategoryId = efCore.Id,
                CreatedAt = now.AddDays(-1)
            },
            new Post
            {
                Title = "Enforcing Data Integrity with Unique Indexes and Restrict Delete Rules",
                Slug = "enforcing-data-integrity-with-unique-indexes-and-restrict-delete-rules",
                Excerpt = "Protect relational consistency at the SQL Server schema level.",
                Content = "Application checks alone cannot prevent concurrent race conditions. Database unique indexes and DeleteBehavior.Restrict act as the final safety boundary.",
                CategoryId = sqlServer.Id,
                CreatedAt = now
            });

        await context.SaveChangesAsync();
    }
}
