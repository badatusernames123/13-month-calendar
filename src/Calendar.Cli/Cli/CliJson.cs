using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calendar.Cli.Cli;

internal static class CliJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void WriteSuccess(object payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            ok = true,
            result = payload,
        }, JsonOptions));
    }

    public static void WriteError(string code, string message)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            ok = false,
            error = new
            {
                code,
                message,
            },
        }, JsonOptions));
    }
}
