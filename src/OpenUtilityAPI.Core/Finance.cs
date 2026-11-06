namespace OpenUtilityAPI.Core;

public static class Finance
{
    public static InterestResult Interest(decimal principal, decimal ratePerPeriod, int periods,
        int decimalPlaces, bool compound)
    {
        Validate(principal, ratePerPeriod, periods, decimalPlaces, 0);
        var factor = compound ? Power(1 + ratePerPeriod, periods) : 1 + ratePerPeriod * periods;
        return new(principal, ratePerPeriod, periods, Round(principal * factor, decimalPlaces),
            Round(principal * (factor - 1), decimalPlaces), "input currency",
            new(compound ? "M = P * (1 + i)^n" : "M = P * (1 + i * n)", decimalPlaces));
    }

    public static LoanResult Loan(decimal principal, decimal ratePerPeriod, int periods,
        int decimalPlaces, bool price)
    {
        Validate(principal, ratePerPeriod, periods, decimalPlaces, 1);
        if (principal != Round(principal, decimalPlaces))
            throw new ArgumentException("Principal must fit the requested decimal places.", nameof(principal));

        // The annuity factor is the sum of discount factors. This also handles zero and tiny rates.
        var discount = 1m;
        var annuityFactor = 0m;
        for (var period = 1; period <= periods; period++)
        {
            discount /= 1 + ratePerPeriod;
            annuityFactor += discount;
        }

        var payment = principal / annuityFactor;
        var exactBalance = principal;
        var balance = principal;
        var installments = new List<Installment>(periods);
        for (var period = 1; period <= periods; period++)
        {
            var exactInterest = exactBalance * ratePerPeriod;
            exactBalance = price ? exactBalance + exactInterest - payment : principal * (periods - period) / periods;
            var nextBalance = period == periods ? 0 : Round(exactBalance, decimalPlaces);
            var amortization = balance - nextBalance;
            var interest = Round(exactInterest, decimalPlaces);
            installments.Add(new(period, amortization, interest, amortization + interest, nextBalance));
            balance = nextBalance;
        }

        return new(principal, ratePerPeriod, periods, installments.Sum(row => row.Payment),
            installments.Sum(row => row.Interest), installments, "input currency",
            new(price ? "A = P / sum((1 + i)^-k, k = 1..n)" : "Amortization = P / n", decimalPlaces));
    }

    public static PercentageResult Percentage(string operation, decimal value, decimal operand, int decimalPlaces)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(value), 1_000_000_000_000m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(operand), 1_000_000_000_000m);
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, 6);
        var (result, unit, formula) = operation switch
        {
            "percentOf" => (value * operand / 100, "input units", "value * operand / 100"),
            "ratioPercent" when operand != 0 => (value / operand * 100, "%", "value / operand * 100"),
            "changePercent" when value != 0 => ((operand - value) / Math.Abs(value) * 100, "%", "(operand - value) / abs(value) * 100"),
            _ => throw new ArgumentException("Use a supported operation and a nonzero divisor.", nameof(operation))
        };
        return new(operation, value, operand, Round(result, decimalPlaces), unit, new(formula, decimalPlaces));
    }

    private static decimal Power(decimal factor, int periods)
    {
        var result = 1m;
        for (var i = 0; i < periods; i++)
            result *= factor;
        return result;
    }

    private static decimal Round(decimal value, int places) => Math.Round(value, places, MidpointRounding.AwayFromZero);

    private static void Validate(decimal principal, decimal rate, int periods, int places, int minimumPeriods)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(principal);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(principal, 1_000_000_000_000m);
        ArgumentOutOfRangeException.ThrowIfNegative(rate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rate, 0.05m);
        ArgumentOutOfRangeException.ThrowIfLessThan(periods, minimumPeriods);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(periods, 360);
        ArgumentOutOfRangeException.ThrowIfNegative(places);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(places, 6);
    }
}

public sealed record Calculation(string Formula, int DecimalPlaces)
{
    public string RoundingMode => nameof(MidpointRounding.AwayFromZero);
}

public sealed record InterestResult(decimal Principal, decimal RatePerPeriod, int Periods,
    decimal FinalAmount, decimal InterestAmount, string Unit, Calculation Calculation);

public sealed record Installment(int Period, decimal Amortization, decimal Interest, decimal Payment, decimal Balance);

public sealed record LoanResult(decimal Principal, decimal RatePerPeriod, int Periods, decimal TotalPayment,
    decimal TotalInterest, IReadOnlyList<Installment> Installments, string Unit, Calculation Calculation);

public sealed record PercentageResult(string Operation, decimal Value, decimal Operand, decimal Result,
    string Unit, Calculation Calculation);
