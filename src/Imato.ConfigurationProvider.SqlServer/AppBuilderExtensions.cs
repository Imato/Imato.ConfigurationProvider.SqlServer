using Microsoft.Extensions.Configuration;

namespace Imato.ConfigurationProvider.SqlServer;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Add configurations from DB table
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="configurationFactory"></param>
    /// <returns></returns>
    public static IConfigurationManager AddSqlConfigurations(
        this IConfigurationManager manager,
        Action<SqlConfigurationOptions>? configurationFactory = null)
    {
        var config = new SqlConfigurationOptions();
        if (configurationFactory != null)
        {
            configurationFactory(config);
        }
        manager.Add(new SqlConfigurationSource(config));
        return manager;
    }
}