using BlogManagement.API.DTOs.Common;
using BlogManagement.API.DTOs.Posts;

namespace BlogManagement.API.Services.Interfaces;

public interface IPostService
{
    Task<PagedResponse<PostSummaryResponse>> GetAllAsync(PostQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<PostResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdatePostRequest request, CancellationToken cancellationToken = default);
    Task PatchAsync(int id, PatchPostRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
