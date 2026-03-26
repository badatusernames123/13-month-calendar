using System.Text.Json;
using System.Text.Json.Serialization;
using Calendar.Core.Domain;

namespace Calendar.Core.Infrastructure;

public sealed class CalendarDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _dataFilePath;

    public CalendarDataStore(string dataFilePath)
    {
        _dataFilePath = Path.GetFullPath(dataFilePath);
    }

    public string DataFilePath => _dataFilePath;

    public async Task<CalendarDataFile> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_dataFilePath))
        {
            return CalendarDataFile.CreateDefault();
        }

        await using var stream = File.OpenRead(_dataFilePath);
        var data = await JsonSerializer.DeserializeAsync<CalendarDataFile>(stream, SerializerOptions, cancellationToken);
        return data ?? CalendarDataFile.CreateDefault();
    }

    public async Task SaveAsync(CalendarDataFile data, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{_dataFilePath}.tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, _dataFilePath, true);
    }

    public Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteLockedAsync(
            async () =>
            {
                await action();
                return true;
            },
            cancellationToken);

    public async Task<T> ExecuteLockedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lockPath = $"{_dataFilePath}.lock";
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(10);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return await action();
            }
            catch (IOException) when (DateTimeOffset.UtcNow < timeoutAt)
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (UnauthorizedAccessException) when (DateTimeOffset.UtcNow < timeoutAt)
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
