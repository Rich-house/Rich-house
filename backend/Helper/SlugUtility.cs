using System.Text;
using System.Text.RegularExpressions;

namespace Marketify.Helper;

public static partial class SlugUtility
{
    public static string GenerateSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character);
        }

        var withoutAccents = builder.ToString().Normalize(NormalizationForm.FormC);
        var slug = NonAlphaNumericPattern().Replace(withoutAccents, "-").Trim('-');
        return DuplicateDashPattern().Replace(slug, "-");
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphaNumericPattern();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex DuplicateDashPattern();
}
