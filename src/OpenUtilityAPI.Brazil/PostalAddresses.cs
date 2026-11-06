namespace OpenUtilityAPI.Brazil;

public static class PostalCodes
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 9 && value[5] == '-')
            value = value.Remove(5, 1);

        if (value.Length != 8 || value.Any(character => character is < '0' or > '9'))
            throw new ArgumentException("Use eight ASCII digits or the standard 00000-000 format.", nameof(value));

        return value;
    }
}

public sealed record PostalAddress(string PostalCode, string Street, string Complement,
    string Neighborhood, string City, string State, string IbgeCode);

public interface IPostalAddressLookup
{
    Task<PostalAddress?> FindAsync(string postalCode, CancellationToken cancellationToken = default);
}

public sealed class PostalLookupException(string message, bool isTimeout = false, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTimeout { get; } = isTimeout;
}
