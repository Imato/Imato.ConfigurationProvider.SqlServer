using System.Reflection;

namespace Imato.ConfigurationProvider.SqlServer;

public class SqlConfigurationOptions
{
	/// <summary>
	/// Connection name in Configurations to configuration DB. Default value: "Configurations"
	/// </summary>
	public string ConnectionStringName { get; set; } = "Configurations";

	/// <summary>
	/// Or connection string to configuration DB. Default value: empty
	/// </summary>
	public string ConnectionString { get; set; } = string.Empty;

	/// <summary>
	/// Table for configuration store. Default value: "Configurations"
	/// </summary>
	public string TableName { get; set; } = "Configurations";

	/// <summary>
	/// Table schema for configuration store. Default value: "dbo"
	/// </summary>
	public string SchemaName { get; set; } = "dbo";

	/// <summary>
	/// Update options interval. Default: 5 min
	/// </summary>
	public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// App name in configuration table. Default: current package id
	/// </summary>
	public string AppName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

	/// <summary>
	/// .NET CORE Environment name from ASPNETCORE_ENVIRONMENT. Default: "Development"
	/// </summary>
	public string EnvironmentName { get; set; } =
		Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
		?? Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")
		?? "Development";

	/// <summary>
	/// Server name. Default: Environment.MachineName
	/// </summary>
	public string ServerName { get; set; } = Environment.MachineName;

	/// <summary>
	/// Save yours local appsettings configuration into DB on start.Default: false
	/// </summary>
	public bool SyncLocalConfigsToDb { get; set; }
}