using Imato.ConfigurationProvider.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public static class Programm
{
	public static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);
		builder.Configuration.AddSqlConfigurations(c =>
		{
			// Connection name in Configurations to configuration DB.
			// Try to use Env variable like ConnectionStrings__Configurations.
			// Default value: "Configurations"
			c.ConnectionStringName = "Configurations";
			// Or use your connection string
			// c.ConnectionString = "";
			// Table for configuration store. Default value: "Configurations"
			// c.TableName = "Configurations";
			// Table sachem for configuration store. Default value: "dbo"
			//c.SchemaName = "dbo";
			// Update options interval. Default: 5 min
			c.RefreshInterval = TimeSpan.FromMinutes(1);
			// App name in configuration table. Default: current package id
			// c.AppName = Assembly.GetEntryAssembly()?.GetName().Name;
			//.NET CORE Environment name from ASPNETCORE_ENVIRONMENT. Default: "Development"
			c.EnvironmentName = "Development";
			// Save your new local appsettings configuration keys into DB on start. Default: false
			c.SyncLocalConfigsToDb = true;
		});

		using var host = builder.Build();

		var configureation = host.Services.GetService<IConfiguration>();
		var provider = configureation.GetConfigurationProvider<SqlConfigurationProvider>();

		while (true)
		{
			var value = configureation.GetValueOf<Test1>();
			Console.WriteLine($"Test1: {JsonSerializer.Serialize(value)}");
			var cs = configureation.GetConnectionString("TestConnection");
			Console.WriteLine($"TestConnection: {cs}");

			provider.Set("Test1:Bar", (value.Bar + 1).ToString());

			await Task.Delay(10_000);
		}

		await host.RunAsync();
	}
}

public record Test1(string Foo, int Bar);