namespace Calendar.Core.Domain;

public sealed record CalendarEvent(string Id, string Title, SolDate Date, string? CategoryId, string? Notes, bool IsAllDay = true);
