using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>Supported Brazilian economic indicators and their latest provider observations.</summary>
[Route("api/v1/br/indicators")]
[Tags("Brazilian indicators")]
public sealed class IndicatorsController(IIndicatorLookup provider, IndicatorCache cache) : UtilityController
{
    /// <summary>List supported indicators, units, observation frequencies and source series.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IndicatorDefinition>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<IndicatorDefinition>> Catalog() => Ok(Indicators.All);

    /// <summary>Get the most recent observation returned by the source for one indicator.</summary>
    /// <param name="code">selic-target, cdi or ipca. Letter case is ignored.</param>
    /// <param name="cancellationToken">Cancels the lookup when the HTTP request ends.</param>
    [HttpGet("{code}")]
    [ProducesResponseType<IndicatorObservation>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<IndicatorObservation>> Latest(string code, CancellationToken cancellationToken)
    {
        if (Indicators.Find(code) is null)
            return Problem(statusCode: 404, title: "Indicator not supported");
        try
        {
            var observation = await cache.GetLatestAsync(code, provider, cancellationToken);
            return observation is null
                ? Problem(statusCode: 404, title: "Indicator observation not available")
                : observation;
        }
        catch (IndicatorLookupException exception)
        {
            return Problem(statusCode: exception.IsTimeout ? 504 : 502,
                title: exception.IsTimeout ? "Indicator provider timed out" : "Indicator provider unavailable");
        }
    }
}
