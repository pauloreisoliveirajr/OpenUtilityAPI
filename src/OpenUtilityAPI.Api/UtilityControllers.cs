using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Core;

namespace OpenUtilityAPI.Api;

/// <summary>Unit and number conversions.</summary>
[Route("api/v1/converters")]
[Tags("Converters")]
public sealed class ConvertersController : UtilityController
{
    /// <summary>Convert between supported length, mass or temperature units.</summary>
    [HttpPost("units")]
    [ProducesResponseType<UnitConversion>(StatusCodes.Status200OK)]
    public ActionResult<UnitConversion> Units(UnitRequest request) =>
        Calculate(() => UnitConverter.Convert(request.Value, request.FromUnit, request.ToUnit, request.DecimalPlaces));

    /// <summary>Spell out an integer in pt-BR or en-US.</summary>
    [HttpPost("number-to-words")]
    [ProducesResponseType<NumberWordsResponse>(StatusCodes.Status200OK)]
    public ActionResult<NumberWordsResponse> Words(NumberWordsRequest request) =>
        Calculate(() => new NumberWordsResponse(request.Number, request.Culture,
            NumberWords.Convert(request.Number, request.Culture)));
}

/// <summary>Gregorian calendar operations on ISO dates.</summary>
[Route("api/v1/datetime")]
[Tags("Date and time")]
public sealed class DatesController : UtilityController
{
    /// <summary>Return the signed number of calendar days from start to end.</summary>
    [HttpPost("difference")]
    [ProducesResponseType<DateDifference>(StatusCodes.Status200OK)]
    public ActionResult<DateDifference> Difference(DateDifferenceRequest request) =>
        new DateDifference(request.Start, request.End, CalendarUtilities.Difference(request.Start, request.End));

    /// <summary>Move by business days, excluding the start, weekends and supplied holidays.</summary>
    [HttpPost("add-business-days")]
    [ProducesResponseType<BusinessDaysResponse>(StatusCodes.Status200OK)]
    public ActionResult<BusinessDaysResponse> Add(BusinessDaysRequest request, CancellationToken cancellationToken) =>
        Calculate(() => new BusinessDaysResponse(request.Start, request.Days,
            CalendarUtilities.AddBusinessDays(request.Start, request.Days, request.Holidays, cancellationToken)));
}

/// <summary>Text transformations and email syntax checks.</summary>
[Route("api/v1/text")]
[Tags("Text")]
public sealed class TextController : UtilityController
{
    /// <summary>Apply selected whitespace, accent and case transformations.</summary>
    [HttpPost("normalize")]
    [ProducesResponseType<NormalizedText>(StatusCodes.Status200OK)]
    public ActionResult<NormalizedText> Normalize(TextRequest request) =>
        Calculate(() => new NormalizedText(TextUtilities.Normalize(request.Text, request.Trim,
            request.CollapseWhitespace, request.RemoveAccents, request.LetterCase)));

    /// <summary>Check ASCII email syntax without performing network lookups.</summary>
    [HttpPost("email/validate")]
    [ProducesResponseType<EmailValidation>(StatusCodes.Status200OK)]
    public ActionResult<EmailValidation> Email(EmailRequest request)
    {
        var valid = TextUtilities.IsEmailSyntaxValid(request.Value);
        return new EmailValidation(valid, valid ? null : "Unsupported email syntax.");
    }
}

/// <summary>Conversion input and optional output precision.</summary>
public sealed record UnitRequest(
    [property: JsonRequired] [DefaultValue(32)] decimal Value,
    [Required, StringLength(3), DefaultValue("F")] string FromUnit,
    [Required, StringLength(3), DefaultValue("C")] string ToUnit,
    [Range(0, 12), DefaultValue(6)] int DecimalPlaces = 6);

/// <summary>Integer and output culture.</summary>
public sealed record NumberWordsRequest(
    [property: JsonRequired] [Range(-999999, 999999), DefaultValue(42)] int Number,
    [Required, RegularExpression("pt-BR|en-US"), DefaultValue("pt-BR")] string Culture);

/// <summary>Localized integer representation.</summary>
public sealed record NumberWordsResponse(int Number, string Culture, string Words);

/// <summary>Start and end dates in YYYY-MM-DD format.</summary>
public sealed record DateDifferenceRequest(
    [property: JsonRequired] [DefaultValue("2024-02-28")] DateOnly Start,
    [property: JsonRequired] [DefaultValue("2024-03-01")] DateOnly End);

/// <summary>Signed difference in calendar days.</summary>
public sealed record DateDifference(DateOnly Start, DateOnly End, int Days);

/// <summary>Start date, signed offset and optional excluded dates.</summary>
public sealed record BusinessDaysRequest(
    [property: JsonRequired] [DefaultValue("2024-02-29")] DateOnly Start,
    [property: JsonRequired] [Range(-10000, 10000), DefaultValue(2)] int Days,
    [MaxLength(366)] DateOnly[]? Holidays = null);

/// <summary>Business-day calculation and resulting date.</summary>
public sealed record BusinessDaysResponse(DateOnly Start, int Days, DateOnly Result);

/// <summary>Text and optional transformations.</summary>
public sealed record TextRequest(
    [property: JsonRequired] [Required(AllowEmptyStrings = true), StringLength(10000), DefaultValue(" Café  com leite ")] string Text,
    bool Trim = false,
    bool CollapseWhitespace = false,
    bool RemoveAccents = false,
    [Required, RegularExpression("preserve|lower|upper")] string LetterCase = "preserve");

/// <summary>Transformed text.</summary>
public sealed record NormalizedText(string Text);

/// <summary>Email address to validate.</summary>
public sealed record EmailRequest([Required, StringLength(254), DefaultValue("person@example.com")] string Value);

/// <summary>Email syntax result and optional reason.</summary>
public sealed record EmailValidation(bool IsValid, string? Reason);
