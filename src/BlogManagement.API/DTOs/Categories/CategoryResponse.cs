namespace BlogManagement.API.DTOs.Categories;

public record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);
