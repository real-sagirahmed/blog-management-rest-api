using System.ComponentModel.DataAnnotations;
using BlogManagement.API.DTOs.Posts;

namespace BlogManagement.Tests.Helpers;

public class PostQueryParametersTests
{
    [Fact]
    public void ArchitecturalLocks_MatchSpecificationInvariants()
    {
        Assert.Equal(100, PostQueryParameters.MaxPageSize);
        Assert.Equal(["createdAt", "title"], PostQueryParameters.AllowedSortFields);
        Assert.Equal(["asc", "desc"], PostQueryParameters.AllowedSortOrders);
    }

    [Fact]
    public void Validate_DefaultParameters_Pass()
    {
        var parameters = new PostQueryParameters();
        var results = ValidateParameters(parameters);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0, 10, "createdAt", "desc")]
    [InlineData(1, 0, "createdAt", "desc")]
    [InlineData(1, 101, "createdAt", "desc")]
    [InlineData(1, 10, "slug", "desc")]
    [InlineData(1, 10, "createdAt", "random")]
    public void Validate_InvalidQueryParameters_ReturnErrors(
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        var parameters = new PostQueryParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var results = ValidateParameters(parameters);

        Assert.NotEmpty(results);
    }

    private static List<ValidationResult> ValidateParameters(PostQueryParameters parameters)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(parameters, new ValidationContext(parameters), results, validateAllProperties: true);
        return results;
    }
}
