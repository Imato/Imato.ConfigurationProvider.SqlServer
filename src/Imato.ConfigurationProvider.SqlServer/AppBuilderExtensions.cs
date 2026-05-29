using Microsoft.Extensions.Configuration;

namespace Imato.ConfigurationProvider.SqlServer;

public static class AppBuilderExtensions
{
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