using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>Public Brazilian postal address lookups.</summary>
[Route("api/v1/br/addresses")]
[Tags("Brazilian addresses")]
public sealed class PostalAddressesController(IPostalAddressLookup provider, PostalAddressCache cache) : UtilityController
{
    /// <summary>Look up a public address by CEP using ViaCEP with bounded local caching.</summary>
    /// <param name="postalCode">Eight ASCII digits or a standard masked CEP, for example 01001-000.</param>
    /// <param name="cancellationToken">Cancels the lookup when the HTTP request ends.</param>
    [HttpGet("{postalCode}")]
    [ProducesResponseType<PostalAddress>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<PostalAddress>> Find(string postalCode, CancellationToken cancellationToken)
    {
        string normalized;
        try { normalized = PostalCodes.Normalize(postalCode); }
        catch (ArgumentException)
        {
            ModelState.AddModelError(nameof(postalCode), "Use eight ASCII digits or the standard 00000-000 format.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var address = await cache.FindAsync(normalized, provider, cancellationToken);
            return address is null
                ? Problem(statusCode: 404, title: "Postal code not found")
                : address;
        }
        catch (PostalLookupException exception)
        {
            return Problem(statusCode: exception.IsTimeout ? 504 : 502,
                title: exception.IsTimeout ? "Postal address provider timed out" : "Postal address provider unavailable");
        }
    }
}
