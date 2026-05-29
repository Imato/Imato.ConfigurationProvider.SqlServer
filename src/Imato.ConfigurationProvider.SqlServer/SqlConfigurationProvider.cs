using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Imato.ConfigurationProvider.SqlServer;

public class SqlConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
{
    private DateTimeOffset _lastLoad = DateTime.UnixEpoch;
    private readonly Timer _timer;
    private readonly SqlConfigurationOptions _options;
    private readonly IConfigurationRoot? _configurationRoot;
    private static object _locker = new object();

    public SqlConfigurationProvider(SqlConfigurationOptions options, IConfigurationRoot? configurationRoot = null)
    {
        _options = options;
        _configurationRoot = configurationRoot;
        _timer = new Timer(_ => Task.Run(() => Load()), null, options.RefreshInterval, options.RefreshInterval);
    }

    public void Init()
    {
        CreateTable();
        SyncLocal();
    }

    public override void Load()
    {
        lock (_locker)
        {
            var changes = 0;
            var sql = $"select [Key], Value, UpdateDate from dbo.Configurations where Value is not null and UpdateDate > @lastLoad and App = @app";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();
                var p1 = new SqlParameter("lastLoad", _lastLoad);
                var p2 = new SqlParameter("app", _options.AppName);
                var command = new SqlCommand(sql, connection);
                command.Parameters.Add(p1);
                command.Parameters.Add(p2);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Data.AddOrUdate(reader["Key"].ToString(), reader["Value"].ToString());
                        var date = reader.GetDateTimeOffset(2);
                        _lastLoad = date > _lastLoad ? date : _lastLoad;
                        changes++;
                    }
                }
            }

            if (changes > 0) OnReload();
        }
    }

    private void CreateTable()
    {
        const string sqlExists = "select 1 as Result from sys.tables t where name = 'Configurations' and t.schema_id = schema_id('dbo')";
        const string sqlCreate = @"
create table dbo.Configurations
	(Id int not null identity(1, 1) constraint Configurations_PK primary key,
	App varchar(255) not null,
	Environment varchar(25) not null,
	[Key] varchar(255) not null,
	Value varchar(max) null,
	PrevValue varchar(max) null,
	UpdateDate datetimeoffset not null,
	UpdateUser varchar(50) not null)
go

alter table dbo.Configurations add constraint Configurations_UK unique (App, Environment, [Key])
go

create trigger Configurations_Insert_TG
	on dbo.Configurations
	instead of insert
as
begin
	insert into dbo.Configurations
		(App, Environment, [Key], Value, UpdateDate, UpdateUser)
	select i.App, i.Environment, i.[Key], i.Value, sysdatetimeoffset(), system_user
		from inserted i
		where not exists
			(select top 1 1
				from dbo.Configurations c
				where  i.App = c.App
					and i.Environment = c.Environment
					and i.[Key] = c.[Key]);
end;
go

create trigger Configurations_Update_TG
	on dbo.Configurations
	instead of update
as
begin
	update c
		set c.PrevValue = c.Value,
				c.Value = i.Value,
				c.UpdateDate = sysdatetimeoffset(),
				c.UpdateUser = system_user
		from dbo.Configurations c
		join  inserted i
			on i.App = c.App
				and i.Environment = c.Environment
				and i.[Key] = c.[Key]
		where c.Value != i.Value;
end;
go

create trigger Configurations_Delete_TG
	on dbo.Configurations
	instead of delete
as
begin
	update c
		set c.PrevValue = c.Value,
			c.Value = null,
			c.UpdateDate = sysdatetimeoffset(),
			c.UpdateUser = system_user
		from dbo.Configurations c
			join deleted d
				on d.App = c.App
					and d.Environment = c.Environment
					and d.[Key] = c.[Key]
		where c.Value is not null;
end;
go
";
        lock (_locker)
        {
            if (_lastLoad > DateTime.UnixEpoch) return;

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlExists, connection);
                var exists = command.ExecuteScalar();
                if (exists?.ToString() == "1") return;

                foreach (var sql in sqlCreate.Split("go", StringSplitOptions.RemoveEmptyEntries))
                {
                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    private void AddParameter(SqlCommand command, string name, string value)
    {
        if (!string.IsNullOrEmpty(name))
        {
            command.Parameters.Add(new(name, value));
        }
    }

    private void Save(string sql, string? key, object? value)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sql)) return;

        var strValue = (value as string) ?? JsonSerializer.Serialize(value);
        using (var connection = new SqlConnection(_options.ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand(sql, connection);
            AddParameter(command, "key", key);
            AddParameter(command, "value", strValue);
            AddParameter(command, "app", _options.AppName);
            AddParameter(command, "env", _options.EnvironmentName);
            command.ExecuteNonQuery();
        }
    }

    private void Add(string? key, object? value)
    {
        const string sql = @"
insert into dbo.Configurations
	(App, Environment, [Key], Value)
values
	(@app, @env, @key, @value);";

        Save(sql, key, value);
    }

    private void Update(string? key, object? value)
    {
        const string sql = @"
update dbo.Configurations
    set Value = @value
    where [Key] = @key
        and Environment = @env
        and App = @app;";

        Save(sql, key, value);
    }

    private void SyncLocal()
    {
        if (_options.SyncLocalConfigsToDb && _configurationRoot != null)
        {
            foreach (var c in _configurationRoot.AsEnumerable())
            {
                foreach (var provider in _configurationRoot.Providers.Where(x => x.GetType().Name == "JsonConfigurationProvider"))
                {
                    if (provider.TryGet(c.Key, out var value))
                    {
                        Add(c.Key, c.Value);
                    }
                }
            }
        }
    }
}