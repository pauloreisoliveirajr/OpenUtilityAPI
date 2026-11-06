using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>Public bank and financial institution directory.</summary>
[Route("api/v1/br/banks")]
[Tags("Brazilian banks")]
public sealed class BanksController(IBankLookup provider, BankDirectoryCache cache) : UtilityController
{
    /// <summary>List institutions, including those without a COMPE code, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Bank>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<IReadOnlyList<Bank>>> List(CancellationToken cancellationToken)
    {
        try { return Ok(await cache.GetBanksAsync(provider, cancellationToken)); }
        catch (BankLookupException exception) { return ProviderProblem(exception); }
    }

    /// <summary>Find an institution by COMPE code, accepting leading zeros.</summary>
    /// <param name="code">One to three ASCII digits representing a code from 1 to 999, for example 001.</param>
    /// <param name="cancellationToken">Cancels the lookup when the HTTP request ends.</param>
    [HttpGet("{code}")]
    [ProducesResponseType<Bank>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<Bank>> Find(string code, CancellationToken cancellationToken)
    {
        int normalized;
        try { normalized = BankCodes.Parse(code); }
        catch (ArgumentException)
        {
            ModelState.AddModelError(nameof(code), "Use a COMPE code from 1 to 999 with up to three ASCII digits.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var banks = await cache.GetBanksAsync(provider, cancellationToken);
            var bank = banks.SingleOrDefault(bank => bank.Code == normalized);
            return bank is null ? Problem(statusCode: 404, title: "Bank code not found") : bank;
        }
        catch (BankLookupException exception) { return ProviderProblem(exception); }
    }

    private ObjectResult ProviderProblem(BankLookupException exception) =>
        Problem(statusCode: exception.IsTimeout ? 504 : 502,
            title: exception.IsTimeout ? "Bank directory provider timed out" : "Bank directory provider unavailable");
}
