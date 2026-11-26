using BlogManagement.API.DTOs.Common;
using BlogManagement.API.DTOs.Posts;
using BlogManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.API.Controllers;

/// <summary>
/// Manages blog posts including search, filtering, sorting, and pagination.
/// </summary>
[ApiController]
[Route("api/posts")]
[Produces("application/json")]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of blog posts with optional search, category filtering, and sorting.
    /// </summary>
    /// <param name="parameters">Query options for search, category filter, pagination, and sorting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated collection of post summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PostSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PostSummaryResponse>>> GetAll(
        [FromQuery] PostQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await postService.GetAllAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific blog post by its ID.
    /// </summary>
    /// <param name="id">The post ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full blog post details.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await postService.GetByIdAsync(id, cancellationToken);
        return Ok(post);
    }

    /// <summary>
    /// Creates a new blog post with a server-generated unique slug.
    /// </summary>
    /// <param name="request">The post creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created post.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PostResponse>> Create(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var created = await postService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Completely updates an existing blog post while preserving its slug.
    /// </summary>
    /// <param name="id">The post ID.</param>
    /// <param name="request">The complete replacement post payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        await postService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Partially updates specific fields of an existing blog post. Omitted fields remain unchanged;
    /// sending explicit null for excerpt clears it.
    /// </summary>
    /// <param name="id">The post ID.</param>
    /// <param name="request">The partial update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
        int id,
        [FromBody] PatchPostRequest request,
        CancellationToken cancellationToken)
    {
        await postService.PatchAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes an existing blog post.
    /// </summary>
    /// <param name="id">The post ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await postService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
