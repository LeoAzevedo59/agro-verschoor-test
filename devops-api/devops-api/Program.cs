using Dapper;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

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

var connection = "Server=devops-db-sql-server;DataBase=master;Uid=sa;Pwd=devops-azevedo#2023;";
builder.Services.AddScoped<DbConnection>(e => new SqlConnection(connection));

//docker run --name container_devops -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=devops-azevedo#2023" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-CU14-ubuntu-20.04

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        return todos;
    }
    catch (Exception ex)
    {
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

    await db.OpenAsync();

    string query = @$"INSERT INTO todos(id, description, isChecked)
                          values('{todo.Id}', '{todo.Description}', {_checked})";

    await db.ExecuteAsync(query);

    await db.CloseAsync();

}).WithName("todos_post");

app.MapDelete("/todos/{id}", async (string id, DbConnection db) =>
{
    await db.OpenAsync();

    string query = @$"delete from todos
                      where id = '{id}'";

    await db.ExecuteAsync(query);

    await db.CloseAsync();
}).WithName("todos_delete");

app.MapPut("/todos/{id}", async (string id, bool isChecked, DbConnection db) =>
{
    int _checked = (isChecked == true ? 1 : 0);

    await db.OpenAsync();
    string query = @$"update todos
                      set isChecked = {_checked}
                      where id = '{id}'";
    await db.ExecuteAsync(query);

    await db.CloseAsync();

}).WithName("todos_put");

app.UseCors();
app.Run();

internal record Todo()
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public bool isChecked { get; set; }
}