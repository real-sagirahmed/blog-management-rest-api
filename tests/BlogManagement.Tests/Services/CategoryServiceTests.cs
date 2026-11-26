using BlogManagement.API.DTOs.Categories;
using BlogManagement.API.Exceptions;
using BlogManagement.API.Models;
using BlogManagement.API.Services.Implementations;
using BlogManagement.Tests.TestHelpers;

namespace BlogManagement.Tests.Services;

public class CategoryServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsCategoriesOrderedByName()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        scope.Context.Categories.AddRange(
            new Category { Name = "Testing", CreatedAt = DateTime.UtcNow },
            new Category { Name = "ASP.NET Core", CreatedAt = DateTime.UtcNow });
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("ASP.NET Core", result[0].Name);
        Assert.Equal("Testing", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsCategory()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = new Category { Name = "EF Core", Description = "ORM notes", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        var result = await service.GetByIdAsync(category.Id);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("EF Core", result.Name);
        Assert.Equal("ORM notes", result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_TrimsAndPersistsCategory()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new CategoryService(scope.Context);

        var created = await service.CreateAsync(new CreateCategoryRequest("  SQL Server  ", "   "));

        Assert.True(created.Id > 0);
        Assert.Equal("SQL Server", created.Name);
        Assert.Null(created.Description);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameCaseInsensitive_ThrowsConflictException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        scope.Context.Categories.Add(new Category { Name = "ASP.NET Core", CreatedAt = DateTime.UtcNow });
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateCategoryRequest("asp.net core", null)));
    }

    [Fact]
    public async Task CreateAsync_WhenTrimmedNameTooShort_ThrowsBadRequestException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(new CreateCategoryRequest(" a ", null)));
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCategory()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = new Category { Name = "Old Name", Description = "Old", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await service.UpdateAsync(category.Id, new UpdateCategoryRequest("New Name", "Updated desc"));

        var updated = await service.GetByIdAsync(category.Id);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Updated desc", updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeepingOwnName_Succeeds()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = new Category { Name = "DotNet", Description = "Initial", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await service.UpdateAsync(category.Id, new UpdateCategoryRequest("DotNet", "Changed desc"));

        var updated = await service.GetByIdAsync(category.Id);
        Assert.Equal("Changed desc", updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameBelongsToAnotherCategory_ThrowsConflictException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var cat1 = new Category { Name = "First", CreatedAt = DateTime.UtcNow };
        var cat2 = new Category { Name = "Second", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.AddRange(cat1, cat2);
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(cat2.Id, new UpdateCategoryRequest("FIRST", null)));
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(999, new UpdateCategoryRequest("Missing", null)));
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasNoPosts_DeletesSuccessfully()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = new Category { Name = "Temp", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await service.DeleteAsync(category.Id);

        Assert.Empty(scope.Context.Categories);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasPosts_ThrowsConflictException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var category = new Category { Name = "ASP.NET Core", CreatedAt = DateTime.UtcNow };
        scope.Context.Categories.Add(category);
        await scope.Context.SaveChangesAsync();

        scope.Context.Posts.Add(new Post
        {
            Title = "Test Post",
            Slug = "test-post",
            Content = "Long enough content for a post.",
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow
        });
        await scope.Context.SaveChangesAsync();

        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(category.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFoundException()
    {
        await using var scope = await SqliteDbContextScope.CreateAsync();
        var service = new CategoryService(scope.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }
}
