using System.ComponentModel.DataAnnotations;

namespace BlogManagement.API.DTOs.Posts;

public sealed class PostQueryParameters : IValidatableObject
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;

    // ARCHITECTURAL INTENT: MaxPageSize = 100 and the strict sort field/order allowlists are locked
    // per BR-09, BR-10, and Section 7.3 to prevent unbounded DB reads and dynamic sort injection.
    public const int MaxPageSize = 100;
    public static readonly IReadOnlyList<string> AllowedSortFields = ["createdAt", "title"];
    public static readonly IReadOnlyList<string> AllowedSortOrders = ["asc", "desc"];

    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int PageNumber { get; init; } = DefaultPageNumber;
    public int PageSize { get; init; } = DefaultPageSize;
    public string SortBy { get; init; } = "createdAt";
    public string SortOrder { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageNumber < 1)
        {
            yield return new ValidationResult("PageNumber must be at least 1.", [nameof(PageNumber)]);
        }

        if (PageSize < 1 || PageSize > MaxPageSize)
        {
            yield return new ValidationResult($"PageSize must be between 1 and {MaxPageSize}.", [nameof(PageSize)]);
        }

        if (CategoryId.HasValue && CategoryId.Value < 1)
        {
            yield return new ValidationResult("CategoryId must be a positive integer.", [nameof(CategoryId)]);
        }

        if (!AllowedSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.",
                [nameof(SortBy)]);
        }

        if (!AllowedSortOrders.Contains(SortOrder, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"SortOrder must be one of: {string.Join(", ", AllowedSortOrders)}.",
                [nameof(SortOrder)]);
        }
    }
}
