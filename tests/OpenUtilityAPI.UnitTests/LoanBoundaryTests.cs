using OpenUtilityAPI.Core;

namespace OpenUtilityAPI.UnitTests;

public sealed class LoanBoundaryTests
{
    public static TheoryData<decimal, decimal, int, int> Cases => new()
    {
        { 0m, 0.05m, 360, 2 },
        { 0.01m, 0.05m, 360, 2 },
        { 1000m, 0m, 360, 2 },
        { 1000m, 0.0000000000000000000000000001m, 360, 2 },
        { 1_000_000_000_000m, 0.05m, 360, 6 },
        { 123m, 0.01m, 1, 0 },
        { 999.999999m, 0.05m, 12, 6 }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void SchedulesReconcileAcrossSupportedBoundaries(decimal principal, decimal rate, int periods, int places)
    {
        foreach (var price in new[] { true, false })
        {
            var loan = Finance.Loan(principal, rate, periods, places, price);
            Assert.Equal(periods, loan.Installments.Count);
            Assert.Equal(0m, loan.Installments[^1].Balance);
            Assert.Equal(principal, loan.Installments.Sum(row => row.Amortization));
            Assert.Equal(loan.TotalPayment, principal + loan.TotalInterest);
            var previousBalance = principal;
            foreach (var row in loan.Installments)
            {
                Assert.InRange(row.Balance, 0m, previousBalance);
                Assert.True(row.Interest >= 0);
                Assert.Equal(previousBalance - row.Balance, row.Amortization);
                Assert.Equal(row.Amortization + row.Interest, row.Payment);
                previousBalance = row.Balance;
            }
        }
    }
}
