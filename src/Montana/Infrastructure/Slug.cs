using System.Globalization;
using System.Text;

namespace Montana.Infrastructure;

internal static class Slug
{
    private const string Fallback = "item";

    /// <summary>
    /// Turns a display name into a URL-safe slug. 
    /// </summary>
    /// <remarks> 
    /// Diacritics are folded ("Café" -> "cafe") rather than dropped.
    /// </remarks>s
    /// <example>"Client: Acme (2026)" -> "client-acme-2026".</example>
    public static string From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Fallback;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var separatorPending = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == 
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }

        return builder.Length == 0 ? Fallback : builder.ToString();
    }
}
