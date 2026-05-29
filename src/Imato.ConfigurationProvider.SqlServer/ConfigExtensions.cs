using Microsoft.Extensions.Configuration;

namespace Imato.ConfigurationProvider.SqlServer;

public static class ConfigExtensions
{
    public static T? GetValueOf<T>(this IConfiguration configuration, string? sectionName = null)
    {
        var name = sectionName ?? typeof(T).Name;
        return configuration.GetRequiredSection(name).Get<T>()
              ?? throw new ArgumentOutOfRangeException($"Unknown configuration value for {name}");
    }
}