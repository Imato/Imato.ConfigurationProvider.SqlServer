#### Imato.ConfigurationProvider.SqlServer

#### Setup
Add startup configuration

```
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
```

and your table TableName from connection ConnectionStringName will be saved and updated every RefreshInterval in IConfiguration.

#### Using
Just get option from DB table in IConfiguration.

```
public record Test1(string Foo, int Bar);

var value = configureation.GetValueOf<Test1>();
Console.WriteLine($"Test1: {JsonSerializer.Serialize(value)}");
```

 