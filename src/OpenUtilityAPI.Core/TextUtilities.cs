using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenUtilityAPI.Core;

public static partial class TextUtilities
{
    [GeneratedRegex(@"\s+", RegexOptions.NonBacktracking)]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"\A[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*\z", RegexOptions.NonBacktracking)]
    private static partial Regex LocalPart();

    [GeneratedRegex(@"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\z", RegexOptions.NonBacktracking)]
    private static partial Regex DomainLabel();

    public static string Normalize(string text, bool trim = false, bool collapseWhitespace = false,
        bool removeAccents = false, string letterCase = "preserve")
    {
        if (text.Length > 10000)
            throw new ArgumentOutOfRangeException(nameof(text));

        if (trim)
            text = text.Trim();
        if (collapseWhitespace)
            text = Whitespace().Replace(text, " ");
        if (removeAccents)
        {
            var result = new StringBuilder();
            foreach (var rune in text.Normalize(NormalizationForm.FormD).EnumerateRunes())
            {
                if (Rune.GetUnicodeCategory(rune) is not UnicodeCategory.NonSpacingMark)
                    result.Append(rune);
            }
            text = result.ToString().Normalize(NormalizationForm.FormC);
        }

        return letterCase switch
        {
            "preserve" => text,
            "lower" => text.ToLowerInvariant(),
            "upper" => text.ToUpperInvariant(),
            _ => throw new ArgumentException("Use preserve, lower or upper.", nameof(letterCase))
        };
    }

    public static bool IsEmailSyntaxValid(string value)
    {
        if (value.Length > 254)
            return false;
        var separator = value.IndexOf('@');
        if (separator is < 1 or > 64 || !LocalPart().IsMatch(value[..separator]))
            return false;

        var labels = value[(separator + 1)..].Split('.');
        return labels.Length >= 2 && labels.All(DomainLabel().IsMatch);
    }
}
