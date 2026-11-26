using System.ComponentModel.DataAnnotations;

namespace BlogManagement.API.DTOs.Categories;

public record CreateCategoryRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Name,

    [StringLength(500)]
    string? Description
);
