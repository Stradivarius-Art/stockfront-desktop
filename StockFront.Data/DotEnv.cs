namespace StockFront.Data;

/// <summary>
/// Minimal, dependency-free <c>.env</c> loader for design-time tooling. Walks up from the
/// current base directory looking for a <c>.env</c> file and copies its <c>KEY=VALUE</c> pairs
/// into process environment variables, without overriding values already present in the real
/// environment (so a CI/host variable always wins over the file).
/// </summary>
public static class DotEnv
{
    /// <summary>Loads the nearest <c>.env</c> file, if one exists. Safe to call repeatedly.</summary>
    public static void Load()
    {
        var path = FindEnvFile();
        if (path is null)
            return;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // Strip optional surrounding single or double quotes.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }
}