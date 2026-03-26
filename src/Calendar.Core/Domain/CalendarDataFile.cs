namespace Calendar.Core.Domain;

public sealed class CalendarDataFile
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public List<CalendarCategory> Categories { get; init; } = [];

    public List<CalendarEvent> Events { get; init; } = [];

    public static CalendarDataFile CreateDefault() =>
        new()
        {
            Categories =
            [
                new CalendarCategory("work", "Work", "#2563EB"),
                new CalendarCategory("personal", "Personal", "#16A34A"),
                new CalendarCategory("holiday", "Holiday", "#DC2626"),
            ],
            Events = [],
        };
}
