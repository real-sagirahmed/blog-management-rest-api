using System.ComponentModel.DataAnnotations;
using BlogManagement.API.DTOs.Common;

namespace BlogManagement.API.DTOs.Posts;

public sealed class PatchPostRequest : IValidatableObject
{
    /// <summary>Updated post title (3–200 characters). Omit to keep unchanged.</summary>
    public Optional<string?> Title { get; init; }

    /// <summary>Updated post excerpt (max 500 characters), or explicit null to clear. Omit to keep unchanged.</summary>
    public Optional<string?> Excerpt { get; init; }

    /// <summary>Updated post body content (min 10 characters). Omit to keep unchanged.</summary>
    public Optional<string?> Content { get; init; }

    /// <summary>Updated category ID (positive integer). Omit to keep unchanged.</summary>
    public Optional<int?> CategoryId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title.HasValue)
        {
            var trimmed = Title.Value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                yield return new ValidationResult("The Title field cannot be null or empty.", [nameof(Title)]);
            }
            else if (trimmed.Length is < 3 or > 200)
            {
                yield return new ValidationResult("The Title field must be between 3 and 200 characters.", [nameof(Title)]);
            }
        }

        if (Excerpt.HasValue && Excerpt.Value is not null && Excerpt.Value.Trim().Length > 500)
        {
            yield return new ValidationResult("The Excerpt field cannot exceed 500 characters.", [nameof(Excerpt)]);
        }

        if (Content.HasValue)
        {
            var trimmed = Content.Value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                yield return new ValidationResult("The Content field cannot be null or empty.", [nameof(Content)]);
            }
            else if (trimmed.Length < 10)
            {
                yield return new ValidationResult("The Content field must be at least 10 characters.", [nameof(Content)]);
            }
        }

        if (CategoryId.HasValue && (!CategoryId.Value.HasValue || CategoryId.Value.Value < 1))
        {
            yield return new ValidationResult("The CategoryId field must be a positive integer and cannot be null.", [nameof(CategoryId)]);
        }
    }
}
