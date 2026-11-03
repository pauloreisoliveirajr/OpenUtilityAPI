using Microsoft.AspNetCore.Mvc;

namespace OpenUtilityAPI.Api;

/// <summary>Shared problem responses for utility endpoints.</summary>
[ApiController]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(429)]
[ProducesResponseType<ProblemDetails>(500)]
public abstract class UtilityController : ControllerBase
{
    /// <summary>Run a calculation and translate invalid inputs to a validation problem.</summary>
    protected ActionResult<T> Calculate<T>(Func<T> calculate)
    {
        try
        {
            return calculate();
        }
        catch (ArgumentException error)
        {
            var field = error.ParamName ?? "request";
            ModelState.AddModelError(field, error.Message);
        }
        catch (OverflowException)
        {
            ModelState.AddModelError("request", "The result exceeds the supported numeric range.");
        }

        return ValidationProblem(ModelState);
    }
}
