using Microsoft.Extensions.Configuration;

namespace Imato.ConfigurationProvider.SqlServer;

public static class ConfigExtensions
{
    /// <summary>
    /// Find class T by name in Configurations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static T? GetValueOf<T>(this IConfiguration configuration, string? sectionName = null)
    {
        var name = sectionName ?? typeof(T).Name;
        return configuration.GetRequiredSection(name).Get<T>()
              ?? throw new ArgumentOutOfRangeException($"Unknown configuration value for {name}");
    }
}