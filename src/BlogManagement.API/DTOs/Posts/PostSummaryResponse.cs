namespace BlogManagement.API.DTOs.Posts;

public record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt
);
