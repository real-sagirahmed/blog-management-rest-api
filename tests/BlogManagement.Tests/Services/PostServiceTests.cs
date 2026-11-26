using BlogManagement.API.DTOs.Common;
using BlogManagement.API.DTOs.Posts;
using BlogManagement.API.Exceptions;
using BlogManagement.API.Models;
using BlogManagement.API.Services.Implementations;
using BlogManagement.Tests.TestHelpers;

namespace BlogManagement.Tests.Services;

public class PostServiceTests
{
    private static async Task<Category> SeedCategoryAsync(SqliteDbContextScope scope, string name = "ASP.NET Core")
    {
        var category = new Category { Name = name, CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsPostWithCategoryName()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = await SeedCategoryAsync(scope);
        var post = new Post
        {
            Title = "EF Core Guide",
            Slug = "ef-core-guide",
            Excerpt = "Short summary",
            Content = "Detailed content about Entity Framework Core.",
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow
        };
        scope.Context.Posts.Add(post);
        await scope.Context.SaveChangesAsync();

        var service = new PostService(scope.Context);

        var result = await service.GetByIdAsync(post.Id);

        Assert.Equal(post.Id, result.Id);
        Assert.Equal("EF Core Guide", result.Title);
        Assert.Equal("ef-core-guide", result.Slug);
        Assert.Equal("ASP.NET Core", result.CategoryName);
        Assert.Null(result.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new PostService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(404));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_GeneratesSlugAndSetsCreatedAt()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);

        var created = await service.CreateAsync(new CreatePostRequest(
            "  Getting Started with ASP.NET Core  ",
            "   ",
            "ASP.NET Core is a modern framework for building APIs.",
            category.Id));

        Assert.True(created.Id > 0);
        Assert.Equal("Getting Started with ASP.NET Core", created.Title);
        Assert.Equal("getting-started-with-asp-net-core", created.Slug);
        Assert.Null(created.Excerpt);
        Assert.Equal(category.Name, created.CategoryName);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Null(created.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentCategoryId_ThrowsBadRequestException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new PostService(scope.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
            new CreatePostRequest("Valid Title", null, "Valid content with enough length.", 999)));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTitles_AppendsIncrementalNumericSuffixes()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);

        var first = await service.CreateAsync(new CreatePostRequest("ASP.NET Core Tips", null, "First article body text here.", category.Id));
        var second = await service.CreateAsync(new CreatePostRequest("ASP.NET Core Tips", null, "Second article body text here.", category.Id));
        var third = await service.CreateAsync(new CreatePostRequest("ASP.NET Core Tips", null, "Third article body text here.", category.Id));

        Assert.Equal("asp-net-core-tips", first.Slug);
        Assert.Equal("asp-net-core-tips-2", second.Slug);
        Assert.Equal("asp-net-core-tips-3", third.Slug);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesFieldsSetsUpdatedAtAndPreservesSlug()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat1 = await SeedCategoryAsync(scope, "Cat 1");
        var cat2 = await SeedCategoryAsync(scope, "Cat 2");
        var service = new PostService(scope.Context);

        var created = await service.CreateAsync(new CreatePostRequest("Original Title", "Old Excerpt", "Original long content body.", cat1.Id));

        await service.UpdateAsync(created.Id, new UpdatePostRequest("Completely New Title", "New Excerpt", "Updated long content body.", cat2.Id));

        var updated = await service.GetByIdAsync(created.Id);
        Assert.Equal("Completely New Title", updated.Title);
        Assert.Equal("original-title", updated.Slug);
        Assert.Equal("New Excerpt", updated.Excerpt);
        Assert.Equal("Updated long content body.", updated.Content);
        Assert.Equal(cat2.Id, updated.CategoryId);
        Assert.Equal("Cat 2", updated.CategoryName);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(999, new UpdatePostRequest("Title", null, "Valid long content.", cat.Id)));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidCategoryId_ThrowsBadRequestException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);
        var created = await service.CreateAsync(new CreatePostRequest("Title", null, "Valid long content.", cat.Id));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateAsync(created.Id, new UpdatePostRequest("New Title", null, "Valid long content.", 999)));
    }

    [Fact]
    public async Task PatchAsync_WhenOnlyTitleSent_UpdatesTitleAndPreservesOtherFieldsAndSlug()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);
        var created = await service.CreateAsync(new CreatePostRequest("Initial Title", "Keep this excerpt", "Keep this content body.", cat.Id));

        var patch = new PatchPostRequest { Title = new Optional<string?>("Patched Title") };
        await service.PatchAsync(created.Id, patch);

        var updated = await service.GetByIdAsync(created.Id);
        Assert.Equal("Patched Title", updated.Title);
        Assert.Equal("initial-title", updated.Slug);
        Assert.Equal("Keep this excerpt", updated.Excerpt);
        Assert.Equal("Keep this content body.", updated.Content);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task PatchAsync_WhenExcerptExplicitNull_ClearsExcerpt()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);
        var created = await service.CreateAsync(new CreatePostRequest("Initial Title", "To be cleared", "Valid content body.", cat.Id));

        var patch = new PatchPostRequest { Excerpt = new Optional<string?>(null) };
        await service.PatchAsync(created.Id, patch);

        var updated = await service.GetByIdAsync(created.Id);
        Assert.Null(updated.Excerpt);
        Assert.Equal("Initial Title", updated.Title);
    }

    [Fact]
    public async Task PatchAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new PostService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.PatchAsync(999, new PatchPostRequest { Title = new Optional<string?>("New Title") }));
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesPost()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var service = new PostService(scope.Context);
        var created = await service.CreateAsync(new CreatePostRequest("To Delete", null, "Valid content body.", cat.Id));

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new PostService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_SupportsPaginationAndMetadata()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope, "Backend");
        var baseTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 1; i <= 5; i++)
        {
            scope.Context.Posts.Add(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                Content = $"Body content for post number {i}.",
                CategoryId = cat.Id,
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        await scope.Context.SaveChangesAsync();

        var service = new PostService(scope.Context);

        var page1 = await service.GetAllAsync(new PostQueryParameters { PageNumber = 1, PageSize = 2 });
        var page3 = await service.GetAllAsync(new PostQueryParameters { PageNumber = 3, PageSize = 2 });
        var emptyPage = await service.GetAllAsync(new PostQueryParameters { PageNumber = 4, PageSize = 2 });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(["Post 5", "Post 4"], page1.Items.Select(x => x.Title));
        Assert.Single(page3.Items);
        Assert.Equal("Post 1", page3.Items[0].Title);
        Assert.Empty(emptyPage.Items);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategoryAndCaseInsensitiveSearch()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var aspNet = await SeedCategoryAsync(scope, "ASP.NET Core");
        var sql = await SeedCategoryAsync(scope, "SQL Server");

        scope.Context.Posts.AddRange(
            new Post
            {
                Title = "REST API Fundamentals",
                Slug = "rest-api-fundamentals",
                Content = "Learn ASP.NET Core Web API design.",
                CategoryId = aspNet.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Post
            {
                Title = "Indexing Deep Dive",
                Slug = "indexing-deep-dive",
                Content = "SQL Server execution plans and ASP.NET Core tips.",
                CategoryId = sql.Id,
                CreatedAt = DateTime.UtcNow
            });
        await scope.Context.SaveChangesAsync();

        var service = new PostService(scope.Context);

        var searchAll = await service.GetAllAsync(new PostQueryParameters { Search = "asp.net" });
        var filtered = await service.GetAllAsync(new PostQueryParameters { Search = "asp.net", CategoryId = aspNet.Id });

        Assert.Equal(2, searchAll.TotalCount);
        Assert.Single(filtered.Items);
        Assert.Equal("REST API Fundamentals", filtered.Items[0].Title);
    }

    [Theory]
    [InlineData("title", "asc", new[] { "Alpha", "Beta", "Gamma" })]
    [InlineData("title", "desc", new[] { "Gamma", "Beta", "Alpha" })]
    [InlineData("createdAt", "asc", new[] { "Gamma", "Alpha", "Beta" })]
    [InlineData("createdAt", "desc", new[] { "Beta", "Alpha", "Gamma" })]
    public async Task GetAllAsync_SortsByRequestedFieldAndDirection(
        string sortBy,
        string sortOrder,
        string[] expectedOrder)
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat = await SeedCategoryAsync(scope);
        var t0 = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        scope.Context.Posts.AddRange(
            new Post { Title = "Gamma", Slug = "gamma", Content = "Content gamma body.", CategoryId = cat.Id, CreatedAt = t0.AddHours(1) },
            new Post { Title = "Alpha", Slug = "alpha", Content = "Content alpha body.", CategoryId = cat.Id, CreatedAt = t0.AddHours(2) },
            new Post { Title = "Beta", Slug = "beta", Content = "Content beta body.", CategoryId = cat.Id, CreatedAt = t0.AddHours(3) });
        await scope.Context.SaveChangesAsync();

        var service = new PostService(scope.Context);

        var result = await service.GetAllAsync(new PostQueryParameters
        {
            SortBy = sortBy,
            SortOrder = sortOrder
        });

        Assert.Equal(expectedOrder, result.Items.Select(x => x.Title));
    }
}
