using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Infrastructure;

public sealed class BrasilApiBankLookup(HttpClient client) : IBankLookup
{
    public async Task<IReadOnlyList<Bank>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.GetAsync("api/banks/v1", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var banks = JsonSerializer.Deserialize<List<BankResponse>>(json)
                ?? throw new JsonException("Missing provider response.");

            if (banks.Count is < 1 or > 1000 ||
                banks.Any(bank => bank is null || bank.Ispb is not { Length: 8 } ||
                    bank.Ispb.Any(character => character is < '0' or > '9') ||
                    string.IsNullOrWhiteSpace(bank.Name) || string.IsNullOrWhiteSpace(bank.FullName) ||
                    bank.Code is < 0 or > 999) ||
                banks.Select(bank => bank.Ispb).Distinct(StringComparer.Ordinal).Count() != banks.Count)
                throw new JsonException("The bank directory contains invalid or duplicate records.");

            var codedBanks = banks.Where(bank => bank.Code is > 0).ToArray();
            if (codedBanks.Select(bank => bank.Code).Distinct().Count() != codedBanks.Length)
                throw new JsonException("The bank directory contains ambiguous COMPE codes.");

            return Array.AsReadOnly(banks.Select(bank => new Bank(bank.Ispb!,
                    bank.Code is > 0 ? bank.Code : null, bank.Name!, bank.FullName!))
                .OrderBy(bank => bank.Name, StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), false))
                .ThenBy(bank => bank.Ispb, StringComparer.Ordinal).ToArray());
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BankLookupException("The bank directory provider timed out.", true, exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new BankLookupException("The bank directory provider returned an unusable response.", innerException: exception);
        }
    }

    private sealed record BankResponse(
        [property: JsonPropertyName("ispb")] string? Ispb,
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("fullName")] string? FullName);
}
