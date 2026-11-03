using OpenUtilityAPI.Core;

namespace OpenUtilityAPI.UnitTests;

public sealed class FinanceTests
{
    [Fact]
    public void CompoundMatchesReference() =>
        Assert.Equal(1126.83m, Finance.Interest(1000, .01m, 12, 2, true).FinalAmount);

    [Fact]
    public void SimpleRoundsMidpointsAwayFromZero() =>
        Assert.Equal(1.01m, Finance.Interest(1, .005m, 1, 2, false).FinalAmount);

    [Theory]
    [InlineData(true, 0, 100)]
    [InlineData(false, 0, 100)]
    [InlineData(true, 1, 102.01)]
    [InlineData(false, 1, 102)]
    public void ScheduleReconciles(bool price, int ratePercent, decimal expectedTotal)
    {
        var loan = Finance.Loan(100, ratePercent / 100m, 3, 2, price);
        Assert.Equal(0, loan.Installments[^1].Balance);
        Assert.Equal(100, loan.Installments.Sum(row => row.Amortization));
        Assert.Equal(expectedTotal, loan.TotalPayment);
        Assert.Equal(loan.TotalPayment, loan.Installments.Sum(row => row.Payment));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(361)]
    public void RejectsInvalidLoanTerm(int periods) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Finance.Loan(100, 0, periods, 2, true));

    [Fact]
    public void HandlesBoundedMaximumCompoundCalculation() =>
        Assert.True(Finance.Interest(1_000_000_000_000m, .05m, 360, 6, true).FinalAmount > 1_000_000_000_000m);

    [Fact]
    public void PercentageHandlesNegativeChangeAndOverflow()
    {
        Assert.Equal(-25, Finance.Percentage("changePercent", 100, 75, 2).Result);
        Assert.Throws<OverflowException>(() => Finance.Percentage("ratioPercent", 1, .0000000000000000000000000001m, 2));
    }
}
