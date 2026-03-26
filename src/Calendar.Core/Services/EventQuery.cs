using Calendar.Core.Domain;

namespace Calendar.Core.Services;

public sealed record EventQuery(
    int? Year = null,
    int? MonthNumber = null,
    int? Day = null,
    SolSpecialDayKind? SpecialDayKind = null,
    string? CategoryId = null);
