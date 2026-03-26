using Calendar.Core.Domain;

namespace Calendar.Core.Services;

public sealed record CalendarSnapshot(
    string DataFilePath,
    IReadOnlyList<CalendarCategory> Categories,
    IReadOnlyList<CalendarEvent> Events);
