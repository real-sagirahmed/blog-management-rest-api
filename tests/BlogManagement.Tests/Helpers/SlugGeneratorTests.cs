using BlogManagement.API.Helpers;

namespace BlogManagement.Tests.Helpers;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Getting Started with ASP.NET Core", "getting-started-with-asp-net-core")]
    [InlineData("C# & .NET 9", "c-net-9")]
    [InlineData("  Multiple   Spaces --- and   Symbols!!!  ", "multiple-spaces-and-symbols")]
    [InlineData("Café Crème Architecture", "cafe-creme-architecture")]
    [InlineData("---!!!---", "post")]
    [InlineData("", "post")]
    public void GenerateBaseSlug_NormalizesExpectedSlugs(string title, string expected)
    {
        var result = SlugGenerator.GenerateBaseSlug(title);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateBaseSlug_RespectsMaxLength()
    {
        var longTitle = new string('a', 250);
        var slug = SlugGenerator.GenerateBaseSlug(longTitle);

        Assert.Equal(SlugGenerator.MaxSlugLength, slug.Length);
    }

    [Fact]
    public void WithNumericSuffix_AppendsSuffixAndRespectsMaxLength()
    {
        var longBase = new string('a', 220);
        var suffixed = SlugGenerator.WithNumericSuffix(longBase, 12);

        Assert.EndsWith("-12", suffixed);
        Assert.True(suffixed.Length <= SlugGenerator.MaxSlugLength);
    }
}
