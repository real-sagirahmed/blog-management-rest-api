using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlogManagement.API.Helpers;

public static partial class SlugGenerator
{
    public const int MaxSlugLength = 220;
    private const string FallbackSlug = "post";

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidSlugCharsRegex();

    public static string GenerateBaseSlug(string title, int maxLength = MaxSlugLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);

        if (string.IsNullOrWhiteSpace(title))
        {
            return FallbackSlug[..Math.Min(FallbackSlug.Length, maxLength)];
        }

        var folded = RemoveDiacritics(title).ToLowerInvariant();
        var slug = InvalidSlugCharsRegex().Replace(folded, "-").Trim('-');

        if (string.IsNullOrEmpty(slug))
        {
            slug = FallbackSlug;
        }

        return TruncateSlug(slug, maxLength);
    }

    public static string WithNumericSuffix(string baseSlug, int suffixNumber, int maxLength = MaxSlugLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(suffixNumber, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 3);

        var suffix = $"-{suffixNumber}";
        var maxBaseLength = maxLength - suffix.Length;
        var safeBase = TruncateSlug(string.IsNullOrWhiteSpace(baseSlug) ? FallbackSlug : baseSlug, maxBaseLength);

        if (string.IsNullOrEmpty(safeBase))
        {
            safeBase = FallbackSlug[..Math.Min(FallbackSlug.Length, maxBaseLength)];
        }

        return $"{safeBase}{suffix}";
    }

    private static string TruncateSlug(string slug, int maxLength)
    {
        if (slug.Length <= maxLength)
        {
            return slug;
        }

        var truncated = slug[..maxLength].TrimEnd('-');
        return string.IsNullOrEmpty(truncated) ? slug[..maxLength] : truncated;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
