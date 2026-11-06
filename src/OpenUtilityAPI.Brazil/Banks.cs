using System.Globalization;

namespace OpenUtilityAPI.Brazil;

public static class BankCodes
{
    public static int Parse(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length is < 1 or > 3 || code.Any(character => character is < '0' or > '9'))
            throw new ArgumentException("Use a COMPE code from 1 to 999 with up to three ASCII digits.", nameof(code));

        var number = int.Parse(code, CultureInfo.InvariantCulture);
        if (number == 0)
            throw new ArgumentException("COMPE code zero does not identify a bank.", nameof(code));
        return number;
    }
}

public sealed record Bank(string Ispb, int? Code, string Name, string FullName);

public interface IBankLookup
{
    Task<IReadOnlyList<Bank>> GetBanksAsync(CancellationToken cancellationToken = default);
}

public sealed class BankLookupException(string message, bool isTimeout = false, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTimeout { get; } = isTimeout;
}
