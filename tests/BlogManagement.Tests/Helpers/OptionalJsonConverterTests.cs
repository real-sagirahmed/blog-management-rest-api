using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BlogManagement.API.DTOs.Posts;

namespace BlogManagement.Tests.Helpers;

public class OptionalJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_WhenFieldOmitted_HasValueIsFalse()
    {
        var dto = JsonSerializer.Deserialize<PatchPostRequest>("""{ "title": "Updated Title" }""", JsonOptions)!;

        Assert.True(dto.Title.HasValue);
        Assert.Equal("Updated Title", dto.Title.Value);
        Assert.False(dto.Excerpt.HasValue);
        Assert.False(dto.Content.HasValue);
        Assert.False(dto.CategoryId.HasValue);
    }

    [Fact]
    public void Deserialize_WhenExcerptExplicitNull_HasValueIsTrueAndValueIsNull()
    {
        var dto = JsonSerializer.Deserialize<PatchPostRequest>("""{ "excerpt": null }""", JsonOptions)!;

        Assert.True(dto.Excerpt.HasValue);
        Assert.Null(dto.Excerpt.Value);
        Assert.Empty(ValidateDto(dto));
    }

    [Fact]
    public void Validate_WhenRequiredFieldsAreExplicitNull_ReturnsValidationErrors()
    {
        var dto = JsonSerializer.Deserialize<PatchPostRequest>(
            """{ "title": null, "content": null, "categoryId": null }""",
            JsonOptions)!;

        var errors = ValidateDto(dto);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(PatchPostRequest.Title)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(PatchPostRequest.Content)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(PatchPostRequest.CategoryId)));
    }

    [Fact]
    public void Validate_WhenTitleTooShort_ReturnsValidationError()
    {
        var dto = JsonSerializer.Deserialize<PatchPostRequest>("""{ "title": "ab" }""", JsonOptions)!;

        var errors = ValidateDto(dto);

        Assert.Single(errors);
        Assert.Contains(nameof(PatchPostRequest.Title), errors[0].MemberNames);
    }

    private static List<ValidationResult> ValidateDto(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }
}
