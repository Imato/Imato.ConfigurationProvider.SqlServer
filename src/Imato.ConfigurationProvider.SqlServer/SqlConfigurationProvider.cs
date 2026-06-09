using AsyncKeyedLock;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Imato.ConfigurationProvider.SqlServer;

/// <summary>
/// Sync app configuration with SQL table
/// </summary>
public class SqlConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
{
	private DateTimeOffset _lastLoad = DateTime.UnixEpoch;
	private readonly Timer _timer;
	private readonly SqlConfigurationOptions _options;
	private readonly IConfigurationRoot? _configurationRoot;
	private static AsyncKeyedLocker<string> _locker = new();

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
		using (var locker = Lock(nameof(Load), 30_000))
		{
			var changes = 0;

			using (var connection = new SqlConnection(_options.ConnectionString))
			{
				connection.Open();

				var command = new SqlCommand(SelectSql, connection);
				AddAppParamaters(command);
				AddParameter(command, "lastLoad", _lastLoad.ToString("o"));

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

	/// <summary>
	/// Update configuration in DB table. If value is null or empty, configuration will be deleted. If configuration value is changed in DB table after last load, exception will be thrown to avoid override changes.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <exception cref="InvalidOperationException">Thrown when the configuration value is changed in the DB table after the last load</exception>
	public override void Set(string key, string? value)
	{
		if (string.IsNullOrEmpty(key)) return;

		using (var locker = Lock(key, 5_000))
		{
			var prevValue = Data.ContainsKey(key) ? Data[key] : null;
			var rows = AddOrUdate(key, value, prevValue);
			if (rows > 0)
			{
				Data[key] = value;
				OnReload();
			}
			else
			{
				throw new InvalidOperationException($"Cannot update configuration for key {key}. It's changed in table. Current value: {prevValue}, new value: {value}");
			}
		}
	}

	private IDisposable Lock(string key, int timeout)
	{
		return _locker.Lock(key);
		// AsyncKeyedLock bug
		var locker = _locker.LockOrNull(nameof(Load), timeout);
		if (locker == null)
			throw new ApplicationException($"Cannot lock key {key}");
		return locker;
	}

	private string SelectSql =>
		$"select [Key], Value, UpdateDate from {_options.SchemaName}.{_options.TableName} where Value is not null and UpdateDate > @lastLoad and App = @app";

	private void CreateTable()
	{
		var sqlExists = $"select 1 as Result from sys.tables t where name = '{_options.TableName}' and t.schema_id = schema_id('{_options.SchemaName}')";
		var sqlCreate = @"
create table {0}.{1}
	(Id int not null identity(1, 1) constraint {0}_{1}_PK primary key,
	App varchar(255) not null,
	Environment varchar(25) not null,
	Server varchar(255) not null,
	[Key] varchar(255) not null,
	Value varchar(max) null,
	PrevValue varchar(max) null,
	UpdateDate datetimeoffset not null,
	UpdateUser varchar(50) not null)
go

alter table {0}.{1} add constraint {0}_{1}_UK unique (App, Environment, Server, [Key])
go

create trigger {0}_{1}_Insert_TG
	on {0}.{1}
	instead of insert
as
begin
	insert into {0}.{1}
		(App, Environment, Server, [Key], Value, UpdateDate, UpdateUser)
	select i.App, i.Environment, i.Server, i.[Key], i.Value, sysdatetimeoffset(), system_user
		from inserted i
		where not exists
			(select top 1 1
				from {0}.{1} c
				where  i.App = c.App
					and i.Environment = c.Environment
					and i.Server = c.Server
					and i.[Key] = c.[Key]);
end;
go

create trigger {0}_{1}_Update_TG
	on {0}.{1}
	instead of update
as
begin
	update c
		set c.PrevValue = c.Value,
				c.Value = i.Value,
				c.UpdateDate = sysdatetimeoffset(),
				c.UpdateUser = system_user
		from {0}.{1} c
		join  inserted i
			on i.App = c.App
				and i.Environment = c.Environment
				and i.Server = c.Server
				and i.[Key] = c.[Key]
		where c.Value != i.Value;
end;
go

create trigger {0}_{1}_Delete_TG
	on {0}.{1}
	instead of delete
as
begin
	update c
		set c.PrevValue = c.Value,
			c.Value = null,
			c.UpdateDate = sysdatetimeoffset(),
			c.UpdateUser = system_user
		from {0}.{1} c
			join deleted d
				on d.App = c.App
					and d.Environment = c.Environment
					and d.Server = c.Server
					and d.[Key] = c.[Key]
		where c.Value is not null;
end;
go
";
		using (var locker = Lock(nameof(CreateTable), 30_000))
		{
			if (_lastLoad > DateTime.UnixEpoch) return;

			using (var connection = new SqlConnection(_options.ConnectionString))
			{
				connection.Open();
				var command = new SqlCommand(sqlExists, connection);
				var exists = command.ExecuteScalar();
				if (exists?.ToString() == "1") return;

				sqlCreate = string.Format(sqlCreate, _options.SchemaName, _options.TableName);
				foreach (var sql in sqlCreate.Split("go", StringSplitOptions.RemoveEmptyEntries))
				{
					command = new SqlCommand(sql, connection);
					command.ExecuteNonQuery();
				}
			}
		}
	}

	private void AddParameter(SqlCommand command, string name, string? value)
	{
		if (!string.IsNullOrEmpty(name))
		{
			if (value != null)
				command.Parameters.Add(new(name, value));
			else
				command.Parameters.Add(new(name, DBNull.Value));
		}
	}

	private int Save(string sql, string? key, string? value, string? prevValue, SqlConnection? connection = null)
	{
		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sql)) return 0;

		var conn = connection ?? new SqlConnection(_options.ConnectionString);
		if (conn.State != System.Data.ConnectionState.Open)
			conn.Open();

		var command = new SqlCommand(string.Format(sql, _options.SchemaName, _options.TableName), conn);
		AddParameter(command, "key", key);
		AddParameter(command, "value", value);
		AddParameter(command, "prevValue", prevValue);
		AddAppParamaters(command);
		var rows = command.ExecuteNonQuery();

		if (connection == null)
		{
			conn.Close();
		}

		return rows;
	}

	private void AddAppParamaters(SqlCommand command)
	{
		AddParameter(command, "app", _options.AppName);
		AddParameter(command, "env", _options.EnvironmentName);
		AddParameter(command, "server", _options.ServerName);
	}

	private int Add(string? key, string? value, SqlConnection? connection = null)
	{
		const string sql = @"
if not exists
	(select top 1 1
		from {0}.{1}
		where [Key] = @key
			and Environment = @env
			and App = @app
			and Server = @server)
insert into {0}.{1}
	(App, Environment, Server, [Key], Value)
values
	(@app, @env, @server, @key, @value);";

		return Save(sql, key, value, null, connection);
	}

	private int Update(string? key, string? value, string? prevValue, SqlConnection? connection = null)
	{
		const string sql = @"
update {0}.{1}
    set Value = @value
    where [Key] = @key
        and Environment = @env
        and App = @app
		and Server = @server
		and Value = @prevValue;";

		return Save(sql, key, value, prevValue, connection);
	}

	/// <summary>
	/// Update configuration in DB table
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <param name="prevValue"></param>
	private int AddOrUdate(string key, string? value, string? prevValue)
	{
		if (string.IsNullOrEmpty(key)) return 0;

		const string sql = @"
if exists
	(select top 1 1
		from {0}.{1}
		where [Key] = @key
			and Environment = @env
			and App = @app
			and Server = @server)
update {0}.{1}
    set Value = @value
    where [Key] = @key
        and Environment = @env
        and App = @app
		and Server = @server
		and Value = @prevValue;
else
    insert into {0}.{1}
	    (App, Environment, Server, [Key], Value)
    values
	    (@app, @env, @server, @key, @value);";

		return Save(sql, key, value, prevValue);
	}

	private void SyncLocal()
	{
		if (_options.SyncLocalConfigsToDb && _configurationRoot != null)
		{
			using var connection = new SqlConnection(_options.ConnectionString);
			foreach (var c in _configurationRoot.AsEnumerable())
			{
				foreach (var provider in _configurationRoot.Providers.Where(x => x.GetType().Name == "JsonConfigurationProvider"))
				{
					if (provider.TryGet(c.Key, out var value))
					{
						Add(c.Key, c.Value, connection);
					}
				}
			}
		}
	}
}