using System.Text.RegularExpressions;

namespace OpenUtilityAPI.Brazil;

public static partial class Documents
{
    [GeneratedRegex(@"\A(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})\z")]
    private static partial Regex CpfPattern();

    [GeneratedRegex(@"\A(?:[A-Za-z0-9]{12}[0-9]{2}|[A-Za-z0-9]{2}\.[A-Za-z0-9]{3}\.[A-Za-z0-9]{3}/[A-Za-z0-9]{4}-[0-9]{2})\z")]
    private static partial Regex CnpjPattern();

    [GeneratedRegex(@"\A(?:[0-9]{11}|[0-9]{3}\.[0-9]{5}\.[0-9]{2}-[0-9])\z")]
    private static partial Regex PisPattern();

    public static string? Normalize(string type, string value)
    {
        var pattern = type switch
        {
            "cpf" => CpfPattern(),
            "cnpj" => CnpjPattern(),
            "pis" => PisPattern(),
            _ => null
        };
        return pattern?.IsMatch(value) == true
            ? value.Replace(".", "").Replace("/", "").Replace("-", "").ToUpperInvariant()
            : null;
    }

    public static bool IsValid(string type, string value)
    {
        var digits = Normalize(type, value);
        if (digits is null || digits.All(c => c == digits[0]))
            return false;

        return type switch
        {
            "cpf" => Check(digits, [10, 9, 8, 7, 6, 5, 4, 3, 2]) &&
                     Check(digits, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]),
            "cnpj" => Check(digits, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) &&
                      Check(digits, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]),
            "pis" => Check(digits, [3, 2, 9, 8, 7, 6, 5, 4, 3, 2]),
            _ => false
        };
    }

    public static string? Format(string type, string value)
    {
        var digits = Normalize(type, value);
        if (digits is null)
            return null;

        return type switch
        {
            "cpf" => $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}",
            "cnpj" => $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}",
            _ => null
        };
    }

    private static bool Check(string digits, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += (digits[i] - '0') * weights[i];

        var remainder = sum % 11;
        return digits[weights.Length] - '0' == (remainder < 2 ? 0 : 11 - remainder);
    }
}
