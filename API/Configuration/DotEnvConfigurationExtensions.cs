using Microsoft.Extensions.Configuration;

namespace LibraryM.WebApi.Configuration;

public static class DotEnvConfigurationExtensions
{
    public static IConfigurationBuilder AddOptionalDotEnv(this IConfigurationBuilder configurationBuilder, string filePath)
    {
        if (!File.Exists(filePath))
        {
            return configurationBuilder;
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
        }

        return configurationBuilder.AddInMemoryCollection(values);
    }
}
