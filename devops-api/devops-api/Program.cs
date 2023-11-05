using Dapper;
using System.Data.Common;
using System.Data.SqlClient;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
      policy =>
      {
          policy.AllowAnyOrigin()
             .AllowAnyHeader()
             .AllowAnyMethod();
      });
});

//configure logging first
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
        optional: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Debug()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(ConfigureElasticSink(configuration, environment))
    .Enrich.WithProperty("Environment", environment)
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

static ElasticsearchSinkOptions ConfigureElasticSink(IConfigurationRoot configuration, string environment)
{
    string IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}";

    return new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
    };
}

// Add configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);

var connection = "Server=devops-db-sql-server;DataBase=master;Uid=sa;Pwd=devops-azevedo#2023;";
builder.Services.AddScoped<DbConnection>(e => new SqlConnection(connection));

//docker run --name container_devops -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=devops-azevedo#2023" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-CU14-ubuntu-20.04

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/configInitial", async (DbConnection db) =>
{
    try
    {
        await db.OpenAsync();

        string query = @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'todos')
                         BEGIN
                             CREATE TABLE todos (
	                                            id UNIQUEIDENTIFIER PRIMARY KEY NOT NULL,
	                                            description VARCHAR(64),
                                                isChecked BIT NOT NULL
                                                )

						                        INSERT INTO todos(id, description, isChecked)
                                                VALUES
                                                (NEWID(), 'Clique aqui! teste 1', 1),
                                                (NEWID(), 'Clique aqui! teste 2', 0)    
                         END";

        await db.ExecuteAsync(query);

        await db.CloseAsync();
    }
    catch (Exception ex)
    {
        return false;
    }

    return true;
}).WithName("todos_configInitial");

app.MapGet("/todos", async (DbConnection db) =>
{
    await db.OpenAsync();

    string query = "SELECT * FROM todos";

    try
    {
        var todos = (await db.QueryAsync<Todo>(query)).ToList();
        Log.Information("request GET: /todos success");
        return todos;
    }
    catch (Exception ex)
    {
        Log.Error("request GET: /todos error {ex}", ex.Message);
        throw;
    }
    finally
    {
        await db.CloseAsync();
    }

}).WithName("todos_get");

app.MapPost("/todos", async (Todo todo, DbConnection db) =>
{
    int _checked = (todo.isChecked == true ? 1 : 0);

    try
    {
        await db.OpenAsync();

        string query = @$"INSERT INTO todos(id, description, isChecked)
                          values('{todo.Id}', '{todo.Description}', {_checked})";

        await db.ExecuteAsync(query);
        Log.Information("request POST: /todos success");
    }
    catch (Exception ex)
    {
        Log.Error("request POST: /todos error {ex}", ex.Message);
        throw;
    }
    finally
    {
        await db.CloseAsync();
    }

}).WithName("todos_post");

app.MapDelete("/todos/{id}", async (string id, DbConnection db) =>
{
    try
    {
        await db.OpenAsync();

        string query = @$"delete from todos
                      where id = '{id}'";

        await db.ExecuteAsync(query);
        Log.Information("request DELETE: /todos success");
    }
    catch (Exception ex)
    {
        Log.Error("request DELETE: /todos error {ex}", ex.Message);
        throw;
    }
    finally
    {
        await db.CloseAsync();
    }
}).WithName("todos_delete");

app.MapPut("/todos/{id}", async (string id, bool isChecked, DbConnection db) =>
{
    int _checked = (isChecked == true ? 1 : 0);

    try
    {
        await db.OpenAsync();
        string query = @$"update todos
                      set isChecked = {_checked}
                      where id = '{id}'";
        await db.ExecuteAsync(query);
        Log.Information("request PUT: /todos success");
    }
    catch (Exception ex)
    {
        Log.Error("request PUT: /todos error {ex}", ex.Message);
        throw;
    }
    finally
    {
        await db.CloseAsync();
    }

}).WithName("todos_put");

#region ERROS FORCE

app.MapGet("/notFound", async (DbConnection db) =>
{
    Log.Error("request GET: /notFound");
    return Results.NotFound();

}).WithName("not_found");

app.MapGet("/Unauthorized", async (DbConnection db) =>
{
    Log.Error("request GET: /Unauthorized");
    return Results.Unauthorized();

}).WithName("unauthorized");

#endregion

app.UseCors();
app.Run();

internal record Todo()
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public bool isChecked { get; set; }
}