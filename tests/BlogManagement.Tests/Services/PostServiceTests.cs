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
}
