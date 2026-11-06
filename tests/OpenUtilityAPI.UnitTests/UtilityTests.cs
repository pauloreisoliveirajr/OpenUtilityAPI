using OpenUtilityAPI.Core;

namespace OpenUtilityAPI.UnitTests;

public sealed class UtilityTests
{
    [Fact]
    public void ConvertsScaleAndOffsets()
    {
        Assert.Equal(273.15m, UnitConverter.Convert(32, "F", "K").Result);
        Assert.Equal(-40, UnitConverter.Convert(-40, "C", "F").Result);
        Assert.Equal(1609.344m, UnitConverter.Convert(1, "mi", "m").Result);
        Assert.Equal(0.45359237m, UnitConverter.Convert(1, "lb", "kg", 8).Result);
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitConverter.Convert(-1, "K", "C"));
    }

    [Theory]
    [InlineData(-42, "en-US", "minus forty-two")]
    [InlineData(1100, "pt-BR", "mil e cem")]
    [InlineData(0, "pt-BR", "zero")]
    [InlineData(999999, "en-US", "nine hundred ninety-nine thousand nine hundred ninety-nine")]
    public void SpellsIntegers(int number, string culture, string expected) =>
        Assert.Equal(expected, NumberWords.Convert(number, culture));

    [Fact]
    public void CountsAcrossLeapDayAndWeekends()
    {
        var friday = new DateOnly(2024, 3, 1);
        var monday = new DateOnly(2024, 3, 4);
        Assert.Equal(-2, CalendarUtilities.Difference(friday, new DateOnly(2024, 2, 28)));
        Assert.Equal(new DateOnly(2024, 3, 5), CalendarUtilities.AddBusinessDays(friday, 1, [monday]));
        Assert.Equal(friday, CalendarUtilities.AddBusinessDays(monday, -1));
        Assert.Equal(monday.AddDays(-1), CalendarUtilities.AddBusinessDays(monday.AddDays(-1), 0));
    }

    [Theory]
    [InlineData("user+tag@example.com", true)]
    [InlineData("User <user@example.com>", false)]
    [InlineData("user@-example.com", false)]
    [InlineData("user@example", false)]
    [InlineData("usuário@example.com", false)]
    public void ChecksEmailSyntax(string value, bool expected) =>
        Assert.Equal(expected, TextUtilities.IsEmailSyntaxValid(value));
}
