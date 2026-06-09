using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Imato.ConfigurationProvider.SqlServer;

public static class AppExtensions
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

	/// <summary>
	/// Add configurations from DB table
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="configurationFactory"></param>
	/// <returns></returns>
	public static IHostApplicationBuilder AddSqlConfigurations(
		this IHostApplicationBuilder builder,
		Action<SqlConfigurationOptions>? configurationFactory = null)
	{
		var config = new SqlConfigurationOptions();
		if (configurationFactory != null)
		{
			configurationFactory(config);
		}
		var source = new SqlConfigurationSource(config);
		builder.Configuration.Add(source);
		return builder;
	}

	/// <summary>
	/// Get a specific configuration provider from the configuration providers collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="configuration"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static T GetConfigurationProvider<T>(this IConfiguration configuration) where T : class
	{
		if (configuration is IConfigurationRoot root)
		{
			var provider = root.Providers.FirstOrDefault(p => p is T) as T;
			if (provider != null)
			{
				return provider;
			}
		}
		throw new InvalidOperationException($"Configuration provider {typeof(T).Name} not found in configuration providers.");
	}

	/// <summary>
	/// Update value in SQL DB table if exists, then in memory configuration
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public static void SetValue(this IConfiguration configuration, string key, string value)
	{
		var provider = configuration.GetConfigurationProvider<SqlConfigurationProvider>();
		provider.Set(key, value);
	}
}