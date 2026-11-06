namespace OpenUtilityAPI.Core;

public static class CalendarUtilities
{
    public static int Difference(DateOnly start, DateOnly end) => end.DayNumber - start.DayNumber;

    public static DateOnly AddBusinessDays(DateOnly start, int days, IReadOnlyCollection<DateOnly>? holidays = null,
        CancellationToken cancellationToken = default)
    {
        if (days is < -10000 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(days));
        if (holidays?.Count > 366)
            throw new ArgumentOutOfRangeException(nameof(holidays));

        var excluded = holidays?.ToHashSet();
        var step = Math.Sign(days);
        for (var remaining = Math.Abs(days); remaining > 0;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            start = start.AddDays(step);
            if (start.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && excluded?.Contains(start) != true)
                remaining--;
        }

        return start;
    }
}
