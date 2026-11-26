using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlogManagement.API.DTOs.Categories;
using BlogManagement.API.DTOs.Common;
using BlogManagement.API.DTOs.Posts;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Tests.Integration;

public class PostsEndpointTests : IAsyncLifetime
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

    private async Task<CategoryResponse> CreateCategoryAsync(string name = "ASP.NET Core")
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    [Fact]
    public async Task TC01_To_TC04_And_TC07_CreateGetAndDuplicateSlugFlow()
    {
        var category = await CreateCategoryAsync();

        // TC-04: Create valid post -> 201 Created + server-generated slug
        var create1 = await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest(
            "Getting Started with ASP.NET Core",
            "Intro excerpt",
            "ASP.NET Core is a modern framework for building REST APIs.",
            category.Id));
        Assert.Equal(HttpStatusCode.Created, create1.StatusCode);
        Assert.NotNull(create1.Headers.Location);
        var post1 = (await create1.Content.ReadFromJsonAsync<PostResponse>())!;
        Assert.Equal("getting-started-with-asp-net-core", post1.Slug);

        // TC-07: Duplicate title auto-suffixes slug -> 201 Created
        var create2 = await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest(
            "Getting Started with ASP.NET Core",
            null,
            "Second article with the same title.",
            category.Id));
        Assert.Equal(HttpStatusCode.Created, create2.StatusCode);
        var post2 = (await create2.Content.ReadFromJsonAsync<PostResponse>())!;
        Assert.Equal("getting-started-with-asp-net-core-2", post2.Slug);

        // TC-01: Get all posts returns summary items without content field
        var listResponse = await _client.GetAsync("/api/posts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rawJson = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var firstItem = doc.RootElement.GetProperty("items")[0];
        Assert.False(firstItem.TryGetProperty("content", out _));
        Assert.False(firstItem.TryGetProperty("excerpt", out _));
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());

        // TC-02: Get existing post -> 200 OK
        var getById = await _client.GetAsync($"/api/posts/{post1.Id}");
        Assert.Equal(HttpStatusCode.OK, getById.StatusCode);
        var fetched = (await getById.Content.ReadFromJsonAsync<PostResponse>())!;
        Assert.Equal("Intro excerpt", fetched.Excerpt);

        // TC-03: Get non-existing post -> 404 Not Found
        var missing = await _client.GetAsync("/api/posts/9999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task TC05_And_TC06_ValidationAndInvalidCategoryReturn400()
    {
        // TC-05: Missing Title -> 400 Validation Error
        var missingTitle = await _client.PostAsJsonAsync("/api/posts", new
        {
            excerpt = "Valid",
            content = "Valid content with enough characters.",
            categoryId = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode);
        var validationProblem = await missingTitle.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(validationProblem);
        Assert.Equal("Validation Error", validationProblem.Title);
        Assert.True(validationProblem.Errors.ContainsKey("Title"));

        // TC-06: Non-existent CategoryId -> 400 Bad Request
        var badCategory = await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest(
            "Valid Post Title",
            null,
            "Valid content with enough characters.",
            9999));
        Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
        var problem = await badCategory.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task TC08_To_TC12_UpdatePatchAndDeleteLifecycle()
    {
        var category = await CreateCategoryAsync();
        var createRes = await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest(
            "Initial Post",
            "Initial Excerpt",
            "Initial content body long enough.",
            category.Id));
        var created = (await createRes.Content.ReadFromJsonAsync<PostResponse>())!;

        // TC-08: Update existing post -> 204 No Content, preserves slug, sets updatedAt
        var putRes = await _client.PutAsJsonAsync($"/api/posts/{created.Id}", new UpdatePostRequest(
            "Updated Post Title",
            "Updated Excerpt",
            "Updated complete article content.",
            category.Id));
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        var afterPut = (await _client.GetFromJsonAsync<PostResponse>($"/api/posts/{created.Id}"))!;
        Assert.Equal("Updated Post Title", afterPut.Title);
        Assert.Equal("initial-post", afterPut.Slug);
        Assert.NotNull(afterPut.UpdatedAt);

        // TC-09: Update missing post -> 404
        var missingPut = await _client.PutAsJsonAsync("/api/posts/9999", new UpdatePostRequest(
            "Updated Post Title",
            null,
            "Updated complete article content.",
            category.Id));
        Assert.Equal(HttpStatusCode.NotFound, missingPut.StatusCode);

        // TC-10: PATCH title only -> preserves excerpt; PATCH {"excerpt": null} -> clears excerpt
        var patchTitleRes = await _client.PatchAsync(
            $"/api/posts/{created.Id}",
            new StringContent("""{"title":"Patched Title Only"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, patchTitleRes.StatusCode);

        var afterPatchTitle = (await _client.GetFromJsonAsync<PostResponse>($"/api/posts/{created.Id}"))!;
        Assert.Equal("Patched Title Only", afterPatchTitle.Title);
        Assert.Equal("Updated Excerpt", afterPatchTitle.Excerpt);

        var patchNullExcerptRes = await _client.PatchAsync(
            $"/api/posts/{created.Id}",
            new StringContent("""{"excerpt":null}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, patchNullExcerptRes.StatusCode);

        var afterPatchNullExcerpt = (await _client.GetFromJsonAsync<PostResponse>($"/api/posts/{created.Id}"))!;
        Assert.Null(afterPatchNullExcerpt.Excerpt);

        // PATCH with explicit null for required field Title -> 400
        var patchNullTitleRes = await _client.PatchAsync(
            $"/api/posts/{created.Id}",
            new StringContent("""{"title":null}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, patchNullTitleRes.StatusCode);

        // TC-11: Delete existing post -> 204, then 404
        var deleteRes = await _client.DeleteAsync($"/api/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
        var getDeleted = await _client.GetAsync($"/api/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);

        // TC-12: Delete missing post -> 404
        var deleteMissing = await _client.DeleteAsync("/api/posts/9999");
        Assert.Equal(HttpStatusCode.NotFound, deleteMissing.StatusCode);
    }

    [Fact]
    public async Task TC13_To_TC15_SearchFilterSortAndPagination()
    {
        var aspNet = await CreateCategoryAsync("ASP.NET Core");
        var sql = await CreateCategoryAsync("SQL Server");

        await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest("Zebra ASP.NET Guide", null, "Web API basics and controllers.", aspNet.Id));
        await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest("Alpha ASP.NET Tips", null, "Routing and filters in .NET.", aspNet.Id));
        await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest("Beta ASP.NET Middleware", null, "ProblemDetails exception handling.", aspNet.Id));
        await _client.PostAsJsonAsync("/api/posts", new CreatePostRequest("SQL Indexing", null, "Clustered and nonclustered indexes.", sql.Id));

        // TC-13 + TC-14 + TC-15 combined
        var page1 = await _client.GetFromJsonAsync<PagedResponse<PostSummaryResponse>>(
            $"/api/posts?search=asp.net&categoryId={aspNet.Id}&pageNumber=1&pageSize=2&sortBy=title&sortOrder=asc");

        Assert.NotNull(page1);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(["Alpha ASP.NET Tips", "Beta ASP.NET Middleware"], page1.Items.Select(p => p.Title));

        var page2 = await _client.GetFromJsonAsync<PagedResponse<PostSummaryResponse>>(
            $"/api/posts?search=asp.net&categoryId={aspNet.Id}&pageNumber=2&pageSize=2&sortBy=title&sortOrder=asc");
        Assert.NotNull(page2);
        Assert.Single(page2.Items);
        Assert.Equal("Zebra ASP.NET Guide", page2.Items[0].Title);
    }

    [Theory]
    [InlineData("/api/posts?pageNumber=0")]
    [InlineData("/api/posts?pageSize=0")]
    [InlineData("/api/posts?pageSize=101")]
    [InlineData("/api/posts?sortBy=invalidField")]
    [InlineData("/api/posts?sortOrder=sideways")]
    public async Task GetPosts_WithInvalidQueryParameters_Returns400(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
