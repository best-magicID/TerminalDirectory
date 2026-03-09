using Microsoft.EntityFrameworkCore;
using task.Infrastructure;
using task.Services;

//var builder = WebApplication.CreateBuilder(args);
var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");

// Регистрация DbContext
builder.Services.AddDbContext<DellinDictionaryDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрация фонового сервиса
builder.Services.AddHostedService<TerminalImportService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
app.Run();
