using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Core;

namespace OpenUtilityAPI.Api;

/// <summary>Interest and amortization calculations.</summary>
[Route("api/v1/finance")]
[Tags("Finance")]
public sealed class FinanceController : UtilityController
{
    /// <summary>Calculate simple interest at a fractional rate per period.</summary>
    [HttpPost("simple-interest")]
    [ProducesResponseType<InterestResult>(StatusCodes.Status200OK)]
    public ActionResult<InterestResult> Simple(FinanceRequest request) =>
        Calculate(() => Finance.Interest(request.Principal, request.RatePerPeriod, request.Periods, request.DecimalPlaces, false));

    /// <summary>Calculate compound interest at a fractional rate per period.</summary>
    [HttpPost("compound-interest")]
    [ProducesResponseType<InterestResult>(StatusCodes.Status200OK)]
    public ActionResult<InterestResult> Compound(FinanceRequest request) =>
        Calculate(() => Finance.Interest(request.Principal, request.RatePerPeriod, request.Periods, request.DecimalPlaces, true));

    /// <summary>Simulate a Price amortization schedule.</summary>
    [HttpPost("price")]
    [ProducesResponseType<LoanResult>(StatusCodes.Status200OK)]
    public ActionResult<LoanResult> Price(FinanceRequest request) =>
        Calculate(() => Finance.Loan(request.Principal, request.RatePerPeriod, request.Periods, request.DecimalPlaces, true));

    /// <summary>Simulate a constant-amortization (SAC) schedule.</summary>
    [HttpPost("sac")]
    [ProducesResponseType<LoanResult>(StatusCodes.Status200OK)]
    public ActionResult<LoanResult> Sac(FinanceRequest request) =>
        Calculate(() => Finance.Loan(request.Principal, request.RatePerPeriod, request.Periods, request.DecimalPlaces, false));
}

/// <summary>Percentage operations.</summary>
[Route("api/v1/math")]
[Tags("Math")]
public sealed class MathController : UtilityController
{
    /// <summary>Calculate percentOf, ratioPercent or changePercent.</summary>
    [HttpPost("percentage")]
    [ProducesResponseType<PercentageResult>(StatusCodes.Status200OK)]
    public ActionResult<PercentageResult> Percentage(PercentageRequest request) =>
        Calculate(() => Finance.Percentage(request.Operation, request.Value, request.Operand, request.DecimalPlaces));
}

/// <summary>Inputs for an interest calculation or amortization schedule.</summary>
public sealed record FinanceRequest(
    [property: JsonRequired] [Range(typeof(decimal), "0", "1000000000000", ParseLimitsInInvariantCulture = true), DefaultValue(1000)] decimal Principal,
    [property: JsonRequired] [Range(typeof(decimal), "0", "0.05", ParseLimitsInInvariantCulture = true), DefaultValue(0.01)] decimal RatePerPeriod,
    [property: JsonRequired] [Range(0, 360), DefaultValue(12)] int Periods,
    [Range(0, 6), DefaultValue(2)] int DecimalPlaces = 2);

/// <summary>Operands and rounding for a percentage operation.</summary>
public sealed record PercentageRequest(
    [Required, RegularExpression("percentOf|ratioPercent|changePercent"), DefaultValue("percentOf")] string Operation,
    [property: JsonRequired] [Range(typeof(decimal), "-1000000000000", "1000000000000", ParseLimitsInInvariantCulture = true), DefaultValue(200)] decimal Value,
    [property: JsonRequired] [Range(typeof(decimal), "-1000000000000", "1000000000000", ParseLimitsInInvariantCulture = true), DefaultValue(15)] decimal Operand,
    [Range(0, 6), DefaultValue(2)] int DecimalPlaces = 2);


