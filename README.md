#### Imato.ConfigurationProvider.SqlServer

#### Setup
Add startup configuration

```
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddSqlConfigurations(c =>
{
    // Connection name in Configurations to configuration DB. 
    // Try to use Env variable like ConnectionStrings__Configurations. 
    // Default value: "Configurations"
    c.ConnectionStringName = "Configurations";
    // Or use your connection string
    c.ConnectionString = "";
    // Table for configuration store. Default value: "Configurations"
    c.TableName = "Configurations"; 
    // Table sachem for configuration store. Default value: "dbo"
    c.SchemaName = "dbo"; 
    // Update options interval. Default: 5 min
    c.RefreshInterval = TimeSpan.FromMinutes(5); 
    // App name in configuration table. Default: current package id
    c.AppName = Assembly.GetEntryAssembly()?.GetName().Name;
    //.NET CORE Environment name from ASPNETCORE_ENVIRONMENT. Default: "Development" 
    c.EnvironmentName = "Development"; 
    // Save your new local appsettings configuration keys into DB on start. Default: false
    c.SyncLocalConfigsToDb = true; 
});
```

and your table TableName from connection ConnectionStringName will be saved and updated every RefreshInterval in IConfiguration.  
Table will be created in startup process. Your user should be have permision for create new Table in DB.

#### Using
Just get option from DB table in IConfiguration.

``` 
appsettings.json
{
	"Test1": {
		"Foo": "ddff",
		"Bar": 4
	}
}
```
```
public record Test1(string Foo, int Bar);

var value = configureation.GetValueOf<Test1>();
Console.WriteLine($"Test1: {JsonSerializer.Serialize(value)}");
```

You can update, save same konfiguration key in table

```
var provider = configureation.GetConfigurationProvider<SqlConfigurationProvider>();
provider.Set("Test1:Bar", (value.Bar + 1).ToString());
```

Manual update value in table 
```
update dbo.Configurations
	set Value = '500'
	where [Key] = 'Test1:Bar'
		and App = 'Imato.TestApp'
		and Environment = 'Development';
```

Table track changes: previous value and user name

```
select * 
	from dbo.Configurations 
	where [Key] = 'Test1:Bar'
		and App = 'Imato.TestApp'
		and Environment = 'Development';


Id  App	            Environment Key	        Value	PrevValue   UpdateDate	                        UpdateUser
2   Imato.TestApp   Development Test1:Bar	509     508         2026-06-04 14:53:18.6911166 +03:00  test_user

```


 