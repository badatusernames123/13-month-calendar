namespace Calendar.Cli.Cli;

internal sealed class CommandArguments
{
    private readonly Dictionary<string, List<string>> _options = new(StringComparer.OrdinalIgnoreCase);

    private CommandArguments(IReadOnlyList<string> positionals)
    {
        Positionals = positionals;
    }

    public IReadOnlyList<string> Positionals { get; }

    public static CommandArguments Parse(string[] args)
    {
        var positionals = new List<string>();
        var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        while (index < args.Length)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(current);
                index++;
                continue;
            }

            if (index == args.Length - 1 || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option '{current}' requires a value.");
            }

            var optionValue = args[index + 1];
            if (!options.TryGetValue(current, out var values))
            {
                values = [];
                options[current] = values;
            }

            values.Add(optionValue);
            index += 2;
        }

        var parsed = new CommandArguments(positionals);
        foreach (var pair in options)
        {
            parsed._options[pair.Key] = pair.Value;
        }

        return parsed;
    }

    public bool HasOption(string name) => _options.ContainsKey(name);

    public string? GetOption(string name) => _options.TryGetValue(name, out var values) ? values[^1] : null;

    public string GetRequiredOption(string name)
        => GetOption(name) ?? throw new ArgumentException($"Missing required option '{name}'.");
}
