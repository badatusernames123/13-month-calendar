namespace Calendar.Core.Infrastructure;

public sealed class CalendarStoragePathResolver
{
    public const string DataFileEnvironmentVariable = "CALENDAR_DATA_FILE";

    public string GetDataFilePath(string? overridePath = null)
    {
        var explicitPath = string.IsNullOrWhiteSpace(overridePath)
            ? Environment.GetEnvironmentVariable(DataFileEnvironmentVariable)
            : overridePath;

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(localAppData, "PostScarcity", "Calendar", "calendar-data.json");
    }
}
