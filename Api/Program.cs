using Microsoft.Data.SqlClient;
using QuasarApi.Extensions;
var version = "2026.04.22";
var builder = WebApplication.CreateBuilder(args);

builder.AddArchitectures();
builder.AddServices();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Recuperando info de conex�o ao banco de dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var sqlBuilder = new SqlConnectionStringBuilder(connectionString);
var dataSource = sqlBuilder.DataSource;
var initialCatalog = sqlBuilder.InitialCatalog;
//var userID = sqlBuilder.UserID;
//var password = sqlBuilder.Password;
//var integratedSecurity = sqlBuilder.IntegratedSecurity;
//var encrypt = sqlBuilder.Encrypt;
//var trustServerCertificate = sqlBuilder.TrustServerCertificate;

var app = builder.Build();

app.UseArchitectures();
app.UseServices();

app.MapGet("/", () =>
    $"Quasar Dealer Nova Chevrolet API is running (version date => {version}){Environment.NewLine}{Environment.NewLine}" +
    $"Server name => {dataSource}{Environment.NewLine}" +
    $"Database name => {initialCatalog}{Environment.NewLine}{Environment.NewLine}" +
    $"Active environment => {builder.Environment.EnvironmentName}");

app.MapRoutes(builder);

app.Run();
