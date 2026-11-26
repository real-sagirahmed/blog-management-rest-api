namespace BlogManagement.API.DTOs.Posts;

public record PostResponse(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    string Content,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
