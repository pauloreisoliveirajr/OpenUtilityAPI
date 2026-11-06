using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>Public Brazilian state and municipality lists from IBGE.</summary>
[Route("api/v1/br/localities")]
[Tags("Brazilian localities")]
public sealed class LocalitiesController(ILocalityLookup provider, LocalityCache cache) : UtilityController
{
    /// <summary>List all Brazilian states and the Federal District, ordered by name.</summary>
    [HttpGet("states")]
    [ProducesResponseType<IReadOnlyList<BrazilianState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<ActionResult<IReadOnlyList<BrazilianState>>> States(CancellationToken cancellationToken) =>
        Lookup(() => cache.GetStatesAsync(provider, cancellationToken));

    /// <summary>List municipalities for a Brazilian state abbreviation, ordered by name.</summary>
    /// <param name="state">A state abbreviation such as SP or DF. Lowercase letters are accepted.</param>
    /// <param name="cancellationToken">Cancels the lookup when the HTTP request ends.</param>
    [HttpGet("states/{state}/municipalities")]
    [ProducesResponseType<IReadOnlyList<Municipality>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<ActionResult<IReadOnlyList<Municipality>>> Municipalities(string state, CancellationToken cancellationToken) =>
        Lookup(() => cache.GetMunicipalitiesAsync(state, provider, cancellationToken));

    private async Task<ActionResult<IReadOnlyList<T>>> Lookup<T>(Func<Task<IReadOnlyList<T>>> lookup)
    {
        try { return Ok(await lookup()); }
        catch (ArgumentException)
        {
            ModelState.AddModelError("state", "Use a Brazilian state abbreviation, for example SP or DF.");
            return ValidationProblem(ModelState);
        }
        catch (LocalityLookupException exception)
        {
            return Problem(statusCode: exception.IsTimeout ? 504 : 502,
                title: exception.IsTimeout ? "Locality provider timed out" : "Locality provider unavailable");
        }
    }
}
