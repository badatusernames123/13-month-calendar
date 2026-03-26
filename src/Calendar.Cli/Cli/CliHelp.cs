namespace Calendar.Cli.Cli;

internal static class CliHelp
{
    public static void Write()
    {
        Console.WriteLine(
"""
Calendar CLI

Usage:
  calendar categories list [--data-file PATH]
  calendar categories add --name NAME --color #RRGGBB [--id ID] [--data-file PATH]
  calendar categories update --id ID --name NAME --color #RRGGBB [--data-file PATH]
  calendar categories remove --id ID [--data-file PATH]

  calendar events list [--year YEAR] [--month MONTH] [--day DAY] [--special leap|year] [--category-id ID] [--data-file PATH]
  calendar events add --title TITLE --year YEAR (--month MONTH --day DAY | --special leap|year) [--category-id ID] [--notes TEXT] [--id ID] [--data-file PATH]
  calendar events update --id ID [--title TITLE] [--year YEAR (--month MONTH --day DAY | --special leap|year)] [--category-id ID|--category-id none] [--notes TEXT|--notes none] [--data-file PATH]
  calendar events remove --id ID [--data-file PATH]

Notes:
  Commands return JSON on success and JSON errors on failure.
  The default data file is resolved through CALENDAR_DATA_FILE or LocalApplicationData.
""");
    }
}
