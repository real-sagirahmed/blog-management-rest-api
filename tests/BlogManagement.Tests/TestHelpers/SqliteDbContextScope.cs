using BlogManagement.API.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Tests.TestHelpers;

internal sealed class SqliteDbContextScope : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public BlogDbContext Context { get; }

    private SqliteDbContextScope(SqliteConnection connection, BlogDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public static async Task<SqliteDbContextScope> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BlogDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new SqliteDbContextScope(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
