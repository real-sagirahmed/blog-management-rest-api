using System.Net;
using System.Net.Http.Json;
using BlogManagement.API.DTOs.Categories;
using BlogManagement.API.DTOs.Posts;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Tests.Integration;

public class CategoriesEndpointTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CategoryCrudLifecycle_WorksEndToEnd()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new CreateCategoryRequest("ASP.NET Core", "API tutorials"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(created);
        Assert.Equal("ASP.NET Core", created.Name);

        var list = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        Assert.NotNull(list);
        Assert.Single(list);

        var byId = await _client.GetFromJsonAsync<CategoryResponse>($"/api/categories/{created.Id}");
        Assert.NotNull(byId);
        Assert.Equal("API tutorials", byId.Description);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/categories/{created.Id}",
            new UpdateCategoryRequest("ASP.NET Core 9", "Updated description"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await _client.GetAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task TC17_CreateDuplicateCategoryName_Returns409ProblemDetails()
    {
        var first = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("SQL Server", null));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("sql server", null));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task TC16_DeleteCategoryContainingPosts_Returns409Conflict()
    {
        var catRes = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("EF Core", null));
        var category = (await catRes.Content.ReadFromJsonAsync<CategoryResponse>())!;

        var postRes = await _client.PostAsJsonAsync(
            "/api/posts",
            new CreatePostRequest("EF Core Relationships", null, "Learn DeleteBehavior.Restrict rules.", category.Id));
        Assert.Equal(HttpStatusCode.Created, postRes.StatusCode);

        var deleteRes = await _client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteRes.StatusCode);
        var problem = await deleteRes.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidPayload_Returns400ValidationProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation Error", problem.Title);
        Assert.True(problem.Errors.ContainsKey("Name"));
    }
}
