# PostScarcity Calendar

Cross-platform desktop calendar application built with C#, .NET 9, and Avalonia UI.

## Features

- 13-month calendar with 28 days per month
- `Sol` inserted between June and July
- `Leap Day` outside the month/week cycle in leap years
- `Year Day` outside the month/week cycle at year end
- Month, week, and day views
- All-day event management
- Local JSON persistence shared by the GUI and CLI
- Category colors and category-based filtering
- Year, month, and day progress bars
- Stable CLI commands with JSON output for automation

Default categories:

- `Work` `#2563EB`
- `Personal` `#16A34A`
- `Holiday` `#DC2626`

## Project Structure

- [`src/Calendar.Core`](/D:/dev/PostScarcity/Calendar/src/Calendar.Core): calendar model, validation, storage, shared repository
- [`src/Calendar.App`](/D:/dev/PostScarcity/Calendar/src/Calendar.App): Avalonia desktop application
- [`src/Calendar.Cli`](/D:/dev/PostScarcity/Calendar/src/Calendar.Cli): command-line interface backed by the same core services

## Requirements

- .NET SDK 9 or newer
- Windows or macOS for the desktop app

## Build

```powershell
dotnet build Calendar.slnx
```

## Run The Desktop App

```powershell
dotnet run --project .\src\Calendar.App\Calendar.App.csproj
```

The app stores data in:

- `CALENDAR_DATA_FILE` if that environment variable is set
- otherwise the OS local app data folder, under `PostScarcity/Calendar/calendar-data.json`

## Run The CLI

```powershell
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- help
```

CLI output is JSON on success and JSON on failure.

Examples:

```powershell
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- categories list
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- categories add --name Study --color "#8B5CF6"
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- events add --title "Release planning" --year 2026 --month 7 --day 4 --category-id work --notes "Quarter kickoff"
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- events add --title "Leap review" --year 2028 --special leap --category-id holiday
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- events list --year 2028 --special leap
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- events update --id EVENT_ID --title "Updated title" --notes "Updated notes"
dotnet run --project .\src\Calendar.Cli\Calendar.Cli.csproj -- events remove --id EVENT_ID
```

Use `--data-file PATH` on any command to target a specific JSON file.

## Publish A Windows Executable

```powershell
dotnet publish .\src\Calendar.App\Calendar.App.csproj -c Release -r win-x64 --self-contained false
```

The published desktop build is written to:

- [`src/Calendar.App/bin/Release/net9.0/win-x64/publish`](/D:/dev/PostScarcity/Calendar/src/Calendar.App/bin/Release/net9.0/win-x64/publish)

## Notes

- All writes go through the shared repository and a file lock so the GUI, CLI, and automation tools can safely share the same local data file.
- Category deletion leaves existing events in place and clears their category reference.
- Special days are stored explicitly using `specialDayKind` in the JSON data file.
