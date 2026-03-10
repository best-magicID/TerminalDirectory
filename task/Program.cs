using Microsoft.EntityFrameworkCore;
using TerminalDirectory.Infrastructure;
using TerminalDirectory.Services;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<DellinDictionaryDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHostedService<TerminalImportService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
