using Imato.ConfigurationProvider.SqlServer;
using Microsoft.Extensions.Configuration;

public class SqlConfigurationSource(SqlConfigurationOptions options) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var config = builder.Build();
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            options.ConnectionString = config.GetConnectionString(options.ConnectionStringName) ?? string.Empty;
        }
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ApplicationException($"Add {nameof(options.ConnectionStringName)} or {nameof(options.ConnectionStringName)} in {nameof(SqlConfigurationOptions)}");
        }
        if (string.IsNullOrEmpty(options.AppName))
        {
            throw new ApplicationException($"Specify {nameof(options.AppName)} in {nameof(SqlConfigurationOptions)}");
        }
        var provider = new SqlConfigurationProvider(options, config);
        provider.Init();
        return provider;
    }
}