using System.ComponentModel.DataAnnotations;

namespace BlogManagement.API.DTOs.Posts;

public record UpdatePostRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Title,

    [StringLength(500)]
    string? Excerpt,

    [Required]
    [MinLength(10)]
    string Content,

    [Range(1, int.MaxValue)]
    int CategoryId
);
