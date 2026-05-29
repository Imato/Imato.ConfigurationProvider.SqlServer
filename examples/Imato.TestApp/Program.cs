using Imato.ConfigurationProvider.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;

public static class Programm
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddSqlConfigurations(c =>
        {
            c.ConnectionStringName = "Configurations"; // Connection name in Configurations to configuration DB. Default value: "Configurations"
            c.ConnectionString = "or your connection string";
            c.TableName = "dbo.Configurations"; // Table for configuration store. Default value: "dbo.Configurations"
            c.RefreshInterval = TimeSpan.FromMinutes(5); // Update options interval. Default: 5 min
            c.AppName = Assembly.GetEntryAssembly()?.GetName().Name; // App name in configuration table. Default: current package id
            c.EnvironmentName = "Development"; //.NET CORE Environment name from ASPNETCORE_ENVIRONMENT. Default: "Development"
            c.SyncLocalConfigsToDb = true; // Save yours local appsettings configuration into DB on start.Default: false
        });

        using var host = builder.Build();

        var configureation = host.Services.GetService<IConfiguration>();

        while (true)
        {
            var value = configureation.GetValueOf<Test1>();
            Console.WriteLine($"Test1: {JsonSerializer.Serialize(value)}");
            var value2 = configureation.GetValueOf<object>("Logging");
            Console.WriteLine($"Logging: {JsonSerializer.Serialize(value2)}");
            var cs = configureation.GetConnectionString("TestConnection");
            Console.WriteLine($"TestConnection: {cs}");
            await Task.Delay(10_000);
        }

        await host.RunAsync();
    }
}

public record Test1(string Foo, int Bar);