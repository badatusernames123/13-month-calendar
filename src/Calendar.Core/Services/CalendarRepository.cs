using System.Text.RegularExpressions;
using Calendar.Core.Domain;
using Calendar.Core.Infrastructure;

namespace Calendar.Core.Services;

public sealed class CalendarRepository
{
    private static readonly Regex ColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly CalendarDataStore _store;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public CalendarRepository(CalendarDataStore store)
    {
        _store = store;
    }

    public string DataFilePath => _store.DataFilePath;

    public async Task<CalendarSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            return await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    return new CalendarSnapshot(
                        _store.DataFilePath,
                        data.Categories.OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
                        data.Events
                            .OrderBy(evt => evt.Date.Year)
                            .ThenBy(evt => SolCalendarMath.ToDayOfYear(evt.Date))
                            .ThenBy(evt => evt.Title, StringComparer.OrdinalIgnoreCase)
                            .ToArray());
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(EventQuery query, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.Events
            .Where(evt => query.Year is null || evt.Date.Year == query.Year.Value)
            .Where(evt => query.MonthNumber is null || evt.Date.MonthNumber == query.MonthNumber.Value)
            .Where(evt => query.Day is null || evt.Date.Day == query.Day.Value)
            .Where(evt => query.SpecialDayKind is null || evt.Date.SpecialDayKind == query.SpecialDayKind.Value)
            .Where(evt => string.IsNullOrWhiteSpace(query.CategoryId) || string.Equals(evt.CategoryId, query.CategoryId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public async Task<CalendarCategory> AddCategoryAsync(string name, string colorHex, string? id = null, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            return await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    var normalizedName = NormalizeRequiredText(name, nameof(name));
                    var normalizedColor = NormalizeColor(colorHex);
                    var normalizedId = NormalizeIdentifier(id) ?? Slugify(normalizedName);

                    if (data.Categories.Any(category => string.Equals(category.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"Category '{normalizedId}' already exists.");
                    }

                    if (data.Categories.Any(category => string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"A category named '{normalizedName}' already exists.");
                    }

                    var category = new CalendarCategory(normalizedId, normalizedName, normalizedColor);
                    data.Categories.Add(category);
                    await _store.SaveAsync(data, cancellationToken);
                    return category;
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CalendarCategory> UpdateCategoryAsync(string id, string name, string colorHex, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            return await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    var existing = data.Categories.FirstOrDefault(category => string.Equals(category.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new KeyNotFoundException($"Category '{id}' was not found.");

                    var normalizedName = NormalizeRequiredText(name, nameof(name));
                    var normalizedColor = NormalizeColor(colorHex);

                    if (data.Categories.Any(category =>
                            !string.Equals(category.Id, existing.Id, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"A category named '{normalizedName}' already exists.");
                    }

                    var updated = existing with { Name = normalizedName, ColorHex = normalizedColor };
                    Replace(data.Categories, category => string.Equals(category.Id, existing.Id, StringComparison.OrdinalIgnoreCase), updated);
                    await _store.SaveAsync(data, cancellationToken);
                    return updated;
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task RemoveCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    var existing = data.Categories.FirstOrDefault(category => string.Equals(category.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new KeyNotFoundException($"Category '{id}' was not found.");

                    data.Categories.Remove(existing);

                    for (var index = 0; index < data.Events.Count; index++)
                    {
                        if (string.Equals(data.Events[index].CategoryId, id, StringComparison.OrdinalIgnoreCase))
                        {
                            data.Events[index] = data.Events[index] with { CategoryId = null };
                        }
                    }

                    await _store.SaveAsync(data, cancellationToken);
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CalendarEvent> AddEventAsync(string title, SolDate date, string? categoryId, string? notes, string? id = null, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            return await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    SolCalendarMath.Validate(date);
                    ValidateCategory(data, categoryId);

                    var normalizedTitle = NormalizeRequiredText(title, nameof(title));
                    var normalizedNotes = NormalizeOptionalText(notes);
                    var normalizedId = NormalizeIdentifier(id) ?? Guid.NewGuid().ToString("N");

                    if (data.Events.Any(evt => string.Equals(evt.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"Event '{normalizedId}' already exists.");
                    }

                    var calendarEvent = new CalendarEvent(normalizedId, normalizedTitle, date, NormalizeIdentifier(categoryId), normalizedNotes);
                    data.Events.Add(calendarEvent);
                    await _store.SaveAsync(data, cancellationToken);
                    return calendarEvent;
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CalendarEvent> UpdateEventAsync(string id, string title, SolDate date, string? categoryId, string? notes, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            return await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    var existing = data.Events.FirstOrDefault(evt => string.Equals(evt.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new KeyNotFoundException($"Event '{id}' was not found.");

                    SolCalendarMath.Validate(date);
                    ValidateCategory(data, categoryId);

                    var updated = existing with
                    {
                        Title = NormalizeRequiredText(title, nameof(title)),
                        Date = date,
                        CategoryId = NormalizeIdentifier(categoryId),
                        Notes = NormalizeOptionalText(notes),
                    };

                    Replace(data.Events, evt => string.Equals(evt.Id, id, StringComparison.OrdinalIgnoreCase), updated);
                    await _store.SaveAsync(data, cancellationToken);
                    return updated;
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task RemoveEventAsync(string id, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            await _store.ExecuteLockedAsync(
                async () =>
                {
                    var data = await EnsureLoadedAsync(cancellationToken);
                    var existing = data.Events.FirstOrDefault(evt => string.Equals(evt.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new KeyNotFoundException($"Event '{id}' was not found.");

                    data.Events.Remove(existing);
                    await _store.SaveAsync(data, cancellationToken);
                },
                cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<CalendarDataFile> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        var data = await _store.LoadAsync(cancellationToken);
        if (data.Categories.Count == 0)
        {
            data = new CalendarDataFile
            {
                Version = CalendarDataFile.CurrentVersion,
                Categories = CalendarDataFile.CreateDefault().Categories,
                Events = data.Events,
            };
            await _store.SaveAsync(data, cancellationToken);
        }

        return data;
    }

    private static void Replace<T>(IList<T> source, Func<T, bool> predicate, T replacement)
    {
        for (var index = 0; index < source.Count; index++)
        {
            if (predicate(source[index]))
            {
                source[index] = replacement;
                return;
            }
        }
    }

    private static void ValidateCategory(CalendarDataFile data, string? categoryId)
    {
        var normalized = NormalizeIdentifier(categoryId);
        if (normalized is null)
        {
            return;
        }

        if (!data.Categories.Any(category => string.Equals(category.Id, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new KeyNotFoundException($"Category '{normalized}' was not found.");
        }
    }

    private static string NormalizeRequiredText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeColor(string colorHex)
    {
        var value = NormalizeRequiredText(colorHex, nameof(colorHex)).ToUpperInvariant();
        if (!ColorRegex.IsMatch(value))
        {
            throw new ArgumentException("Colors must use #RRGGBB format.", nameof(colorHex));
        }

        return value;
    }

    private static string? NormalizeIdentifier(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Slugify(string value)
    {
        var buffer = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        var slug = new string(buffer).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
