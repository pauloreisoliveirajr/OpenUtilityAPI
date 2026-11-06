namespace OpenUtilityAPI.Core;

public static class UnitConverter
{
    private static readonly Dictionary<string, decimal> Length = new()
    {
        ["mm"] = 0.001m, ["cm"] = 0.01m, ["m"] = 1m, ["km"] = 1000m,
        ["in"] = 0.0254m, ["ft"] = 0.3048m, ["mi"] = 1609.344m
    };

    private static readonly Dictionary<string, decimal> Mass = new()
    {
        ["g"] = 0.001m, ["kg"] = 1m, ["lb"] = 0.45359237m, ["oz"] = 0.028349523125m
    };

    public static UnitConversion Convert(decimal value, string from, string to, int decimalPlaces = 6)
    {
        if (decimalPlaces is < 0 or > 12)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        if (value is < -1_000_000_000_000m or > 1_000_000_000_000m)
            throw new ArgumentOutOfRangeException(nameof(value));

        decimal result;
        string formula;
        if (from is "C" or "F" or "K" && to is "C" or "F" or "K")
        {
            var celsius = from switch
            {
                "F" => (value - 32) * 5 / 9,
                "K" => value - 273.15m,
                _ => value
            };

            if (celsius < -273.15m)
                throw new ArgumentOutOfRangeException(nameof(value), "Temperature is below absolute zero.");

            result = from == to ? value : to switch
            {
                "F" => celsius * 9 / 5 + 32,
                "K" => celsius + 273.15m,
                _ => celsius
            };
            formula = "Convert via Celsius using the scale factor and offset for each unit.";
        }
        else
        {
            var units = Length.ContainsKey(from) ? Length : Mass;
            if (!units.TryGetValue(from, out var source) || !units.TryGetValue(to, out var target))
                throw new ArgumentException("Select supported units in the same category.", nameof(to));

            result = value * source / target;
            formula = "value * source factor / target factor";
        }

        return new(value, from, to, Math.Round(result, decimalPlaces, MidpointRounding.AwayFromZero),
            new Calculation(formula, decimalPlaces));
    }
}

public sealed record UnitConversion(decimal Value, string FromUnit, string ToUnit, decimal Result, Calculation Calculation);
