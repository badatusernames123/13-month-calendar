using System.Globalization;

namespace Calendar.Core.Domain;

public static class SolCalendarMath
{
    public const int DaysPerWeek = 7;
    public const int DaysPerMonth = 28;
    public const int MonthsPerYear = 13;
    public const int DaysBeforeSol = 6 * DaysPerMonth;
    public const int DaysInRegularMonths = MonthsPerYear * DaysPerMonth;

    private static readonly string[] MonthNames =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "Sol",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December",
    ];

    private static readonly string[] DayNames =
    [
        "Sunday",
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
    ];

    public static IReadOnlyList<string> DayNamesInWeek => DayNames;

    public static bool IsLeapYear(int year) => DateTime.IsLeapYear(year);

    public static int GetDaysInYear(int year) => IsLeapYear(year) ? 366 : 365;

    public static string GetMonthName(int monthNumber)
    {
        ValidateMonthNumber(monthNumber);
        return MonthNames[monthNumber - 1];
    }

    public static SolDate FromGregorian(DateOnly date) => FromDayOfYear(date.Year, date.DayOfYear);

    public static DateOnly ToGregorianDate(SolDate date)
    {
        Validate(date);
        return new DateOnly(date.Year, 1, 1).AddDays(ToDayOfYear(date) - 1);
    }

    public static SolDate AddDays(SolDate date, int days)
    {
        var gregorian = ToGregorianDate(date).AddDays(days);
        return FromGregorian(gregorian);
    }

    public static SolDate FromDayOfYear(int year, int dayOfYear)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dayOfYear, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dayOfYear, GetDaysInYear(year));

        if (dayOfYear <= DaysBeforeSol)
        {
            return new SolDate(year, ((dayOfYear - 1) / DaysPerMonth) + 1, ((dayOfYear - 1) % DaysPerMonth) + 1);
        }

        var isLeapYear = IsLeapYear(year);
        if (isLeapYear && dayOfYear == DaysBeforeSol + 1)
        {
            return new SolDate(year, 0, 0, SolSpecialDayKind.LeapDay);
        }

        var startOfSol = DaysBeforeSol + 1 + (isLeapYear ? 1 : 0);
        var endOfRegularMonths = startOfSol + (7 * DaysPerMonth) - 1;

        if (dayOfYear <= endOfRegularMonths)
        {
            var zeroBased = dayOfYear - startOfSol;
            return new SolDate(year, 7 + (zeroBased / DaysPerMonth), (zeroBased % DaysPerMonth) + 1);
        }

        return new SolDate(year, 0, 0, SolSpecialDayKind.YearDay);
    }

    public static int ToDayOfYear(SolDate date)
    {
        Validate(date);

        if (!date.IsSpecialDay)
        {
            if (date.MonthNumber <= 6)
            {
                return ((date.MonthNumber - 1) * DaysPerMonth) + date.Day;
            }

            var startOfSol = DaysBeforeSol + 1 + (IsLeapYear(date.Year) ? 1 : 0);
            return startOfSol + ((date.MonthNumber - 7) * DaysPerMonth) + (date.Day - 1);
        }

        return date.SpecialDayKind switch
        {
            SolSpecialDayKind.LeapDay when IsLeapYear(date.Year) => DaysBeforeSol + 1,
            SolSpecialDayKind.YearDay => GetDaysInYear(date.Year),
            _ => throw new ArgumentOutOfRangeException(nameof(date), "Invalid special day for the specified year."),
        };
    }

    public static IReadOnlyList<SolDate> GetMonthDays(int year, int monthNumber)
    {
        ValidateMonthNumber(monthNumber);
        return Enumerable.Range(1, DaysPerMonth).Select(day => new SolDate(year, monthNumber, day)).ToArray();
    }

    public static IReadOnlyList<SolDate> GetWeekDates(SolDate date)
    {
        Validate(date);

        if (date.IsSpecialDay)
        {
            return [date];
        }

        var weekStartDay = ((date.Day - 1) / DaysPerWeek) * DaysPerWeek + 1;
        return Enumerable.Range(weekStartDay, DaysPerWeek)
            .Select(day => new SolDate(date.Year, date.MonthNumber, day))
            .ToArray();
    }

    public static int GetWeekOfMonth(SolDate date)
    {
        Validate(date);
        return date.IsSpecialDay ? 0 : ((date.Day - 1) / DaysPerWeek) + 1;
    }

    public static string GetDayName(SolDate date)
    {
        Validate(date);

        if (date.IsSpecialDay)
        {
            return date.SpecialDayKind switch
            {
                SolSpecialDayKind.LeapDay => "Leap Day",
                SolSpecialDayKind.YearDay => "Year Day",
                _ => throw new ArgumentOutOfRangeException(nameof(date)),
            };
        }

        return DayNames[(date.Day - 1) % DaysPerWeek];
    }

    public static double GetYearProgress(DateTimeOffset now)
    {
        var start = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, now.Offset);
        var next = start.AddYears(1);
        return Clamp01((now - start).TotalSeconds / (next - start).TotalSeconds);
    }

    public static double GetMonthProgress(DateTimeOffset now)
    {
        var solDate = FromGregorian(DateOnly.FromDateTime(now.DateTime));
        if (solDate.IsSpecialDay)
        {
            return 1.0d;
        }

        return Clamp01(((solDate.Day - 1) + (now.TimeOfDay.TotalSeconds / TimeSpan.FromDays(1).TotalSeconds)) / DaysPerMonth);
    }

    public static double GetDayProgress(DateTimeOffset now) => Clamp01(now.TimeOfDay.TotalSeconds / TimeSpan.FromDays(1).TotalSeconds);

    public static void Validate(SolDate date)
    {
        if (date.Year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "Year must be positive.");
        }

        if (date.IsSpecialDay)
        {
            if (date.MonthNumber != 0 || date.Day != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(date), "Special days do not have a month or day number.");
            }

            if (date.SpecialDayKind == SolSpecialDayKind.LeapDay && !IsLeapYear(date.Year))
            {
                throw new ArgumentOutOfRangeException(nameof(date), "Leap Day is only valid in leap years.");
            }

            return;
        }

        ValidateMonthNumber(date.MonthNumber);
        ArgumentOutOfRangeException.ThrowIfLessThan(date.Day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(date.Day, DaysPerMonth);
    }

    public static string ToInvariantString(double value) => value.ToString("P1", CultureInfo.InvariantCulture);

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);

    private static void ValidateMonthNumber(int monthNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(monthNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthNumber, MonthsPerYear);
    }
}
