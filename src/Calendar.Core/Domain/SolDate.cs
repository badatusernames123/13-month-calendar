using System.Globalization;
using System.Text.Json.Serialization;

namespace Calendar.Core.Domain;

public readonly record struct SolDate(int Year, int MonthNumber, int Day, SolSpecialDayKind SpecialDayKind = SolSpecialDayKind.None)
{
    [JsonIgnore]
    public bool IsSpecialDay => SpecialDayKind != SolSpecialDayKind.None;

    [JsonIgnore]
    public string MonthName => SolCalendarMath.GetMonthName(MonthNumber);

    [JsonIgnore]
    public string ShortLabel => SpecialDayKind switch
    {
        SolSpecialDayKind.LeapDay => $"Leap Day {Year}",
        SolSpecialDayKind.YearDay => $"Year Day {Year}",
        _ => $"{MonthName} {Day}, {Year}",
    };

    [JsonIgnore]
    public string LongLabel => SpecialDayKind switch
    {
        SolSpecialDayKind.LeapDay => $"Leap Day, {Year}",
        SolSpecialDayKind.YearDay => $"Year Day, {Year}",
        _ => $"{MonthName} {Day.ToString(CultureInfo.InvariantCulture)}, {Year}",
    };

    public override string ToString() => ShortLabel;
}
