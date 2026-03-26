using Calendar.Cli.Cli;
using Calendar.Core.Domain;
using Calendar.Core.Infrastructure;
using Calendar.Core.Services;

namespace Calendar.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
            {
                CliHelp.Write();
                return (int)CliExitCode.Success;
            }

            var commandArguments = CommandArguments.Parse(args);
            var repository = CreateRepository(commandArguments.GetOption("--data-file"));

            var exitCode = commandArguments.Positionals.ToArray() switch
            {
                ["categories", "list"] => await ListCategoriesAsync(repository),
                ["categories", "add"] => await AddCategoryAsync(repository, commandArguments),
                ["categories", "update"] => await UpdateCategoryAsync(repository, commandArguments),
                ["categories", "remove"] => await RemoveCategoryAsync(repository, commandArguments),
                ["events", "list"] => await ListEventsAsync(repository, commandArguments),
                ["events", "add"] => await AddEventAsync(repository, commandArguments),
                ["events", "update"] => await UpdateEventAsync(repository, commandArguments),
                ["events", "remove"] => await RemoveEventAsync(repository, commandArguments),
                _ => throw new ArgumentException("Unknown command. Run 'help' for usage."),
            };

            return (int)exitCode;
        }
        catch (KeyNotFoundException exception)
        {
            CliJson.WriteError("not_found", exception.Message);
            return (int)CliExitCode.NotFound;
        }
        catch (ArgumentException exception)
        {
            CliJson.WriteError("invalid_arguments", exception.Message);
            return (int)CliExitCode.InvalidArguments;
        }
        catch (InvalidOperationException exception)
        {
            CliJson.WriteError("validation_error", exception.Message);
            return (int)CliExitCode.ValidationError;
        }
        catch (Exception exception)
        {
            CliJson.WriteError("unexpected_error", exception.Message);
            return (int)CliExitCode.UnexpectedError;
        }
    }

    private static CalendarRepository CreateRepository(string? dataFileOverride)
    {
        var resolver = new CalendarStoragePathResolver();
        var dataFilePath = resolver.GetDataFilePath(dataFileOverride);
        return new CalendarRepository(new CalendarDataStore(dataFilePath));
    }

    private static async Task<CliExitCode> ListCategoriesAsync(CalendarRepository repository)
    {
        var snapshot = await repository.GetSnapshotAsync();
        CliJson.WriteSuccess(new
        {
            snapshot.DataFilePath,
            snapshot.Categories,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> AddCategoryAsync(CalendarRepository repository, CommandArguments arguments)
    {
        var category = await repository.AddCategoryAsync(
            arguments.GetRequiredOption("--name"),
            arguments.GetRequiredOption("--color"),
            arguments.GetOption("--id"));

        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            Category = category,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> UpdateCategoryAsync(CalendarRepository repository, CommandArguments arguments)
    {
        var category = await repository.UpdateCategoryAsync(
            arguments.GetRequiredOption("--id"),
            arguments.GetRequiredOption("--name"),
            arguments.GetRequiredOption("--color"));

        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            Category = category,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> RemoveCategoryAsync(CalendarRepository repository, CommandArguments arguments)
    {
        await repository.RemoveCategoryAsync(arguments.GetRequiredOption("--id"));
        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            RemovedCategoryId = arguments.GetRequiredOption("--id"),
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> ListEventsAsync(CalendarRepository repository, CommandArguments arguments)
    {
        var query = new EventQuery(
            Year: ParseInt(arguments.GetOption("--year"), "--year"),
            MonthNumber: ParseInt(arguments.GetOption("--month"), "--month"),
            Day: ParseInt(arguments.GetOption("--day"), "--day"),
            SpecialDayKind: ParseSpecialDay(arguments.GetOption("--special")),
            CategoryId: NormalizeOptionalValue(arguments.GetOption("--category-id")));

        var events = await repository.GetEventsAsync(query);
        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            Events = events,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> AddEventAsync(CalendarRepository repository, CommandArguments arguments)
    {
        var calendarEvent = await repository.AddEventAsync(
            arguments.GetRequiredOption("--title"),
            ParseDate(arguments),
            NormalizeOptionalValue(arguments.GetOption("--category-id")),
            NormalizeOptionalValue(arguments.GetOption("--notes")),
            arguments.GetOption("--id"));

        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            Event = calendarEvent,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> UpdateEventAsync(CalendarRepository repository, CommandArguments arguments)
    {
        var snapshot = await repository.GetSnapshotAsync();
        var existing = snapshot.Events.FirstOrDefault(evt => string.Equals(evt.Id, arguments.GetRequiredOption("--id"), StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Event '{arguments.GetRequiredOption("--id")}' was not found.");

        var hasDateOptions = arguments.HasOption("--year") || arguments.HasOption("--month") || arguments.HasOption("--day") || arguments.HasOption("--special");
        var updatedDate = hasDateOptions ? ParseDate(arguments) : existing.Date;
        var updatedTitle = arguments.GetOption("--title") ?? existing.Title;
        var updatedCategoryId = NormalizeOptionalValue(arguments.GetOption("--category-id")) switch
        {
            "none" => null,
            var provided when provided is not null => provided,
            _ => existing.CategoryId,
        };
        var updatedNotes = NormalizeOptionalValue(arguments.GetOption("--notes")) switch
        {
            "none" => null,
            var provided when provided is not null => provided,
            _ => existing.Notes,
        };

        var calendarEvent = await repository.UpdateEventAsync(existing.Id, updatedTitle, updatedDate, updatedCategoryId, updatedNotes);
        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            Event = calendarEvent,
        });

        return CliExitCode.Success;
    }

    private static async Task<CliExitCode> RemoveEventAsync(CalendarRepository repository, CommandArguments arguments)
    {
        await repository.RemoveEventAsync(arguments.GetRequiredOption("--id"));
        CliJson.WriteSuccess(new
        {
            repository.DataFilePath,
            RemovedEventId = arguments.GetRequiredOption("--id"),
        });

        return CliExitCode.Success;
    }

    private static SolDate ParseDate(CommandArguments arguments)
    {
        var year = ParseRequiredInt(arguments.GetRequiredOption("--year"), "--year");
        var special = arguments.GetOption("--special");

        if (!string.IsNullOrWhiteSpace(special))
        {
            return new SolDate(year, 0, 0, ParseSpecialDay(special) ?? throw new ArgumentException("Invalid special day."));
        }

        var month = ParseRequiredInt(arguments.GetRequiredOption("--month"), "--month");
        var day = ParseRequiredInt(arguments.GetRequiredOption("--day"), "--day");
        return new SolDate(year, month, day);
    }

    private static SolSpecialDayKind? ParseSpecialDay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "leap" => SolSpecialDayKind.LeapDay,
            "year" => SolSpecialDayKind.YearDay,
            _ => throw new ArgumentException("Special day must be 'leap' or 'year'."),
        };
    }

    private static int? ParseInt(string? value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseRequiredInt(value, optionName);
    }

    private static int ParseRequiredInt(string value, string optionName)
    {
        if (!int.TryParse(value, out var parsed))
        {
            throw new ArgumentException($"Option '{optionName}' must be an integer.");
        }

        return parsed;
    }

    private static string? NormalizeOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
