using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using task.Entities;
using task.Infrastructure;

namespace task.Services;

public class TerminalImportService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TerminalImportService> _logger;
    private const string JsonFile = "files/terminals.json";

    public TerminalImportService(IServiceProvider serviceProvider, ILogger<TerminalImportService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TerminalImportService запуск.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowMsk = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Russian Standard Time");
                var nextRun = nowMsk.Date.AddHours(2); // 02:00 MSK сегодня
                if (nowMsk >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - nowMsk;
                _logger.LogInformation("Next import scheduled in {Hours}h {Minutes}m", delay.Hours, delay.Minutes);
                await Task.Delay(delay, stoppingToken);

                await ImportTerminalsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // приложение останавливается
                _logger.LogInformation("TerminalImportService stopping gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TerminalImportService loop");
                // Ждём минуту перед повторной попыткой
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ImportTerminalsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();

        if (!File.Exists(JsonFile))
        {
            _logger.LogError("JSON file not found: {File}", JsonFile);
            return;
        }

        string json = await File.ReadAllTextAsync(JsonFile, stoppingToken);
        var terminals = JsonSerializer.Deserialize<List<Office>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (terminals == null)
        {
            _logger.LogError("No terminals found in JSON file");
            return;
        }

        _logger.LogInformation("Loaded {Count} terminals from JSON", terminals.Count);

        // Удаляем старые записи
        var oldCount = await db.Offices.CountAsync(stoppingToken);
        db.Offices.RemoveRange(db.Offices);
        await db.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Deleted {OldCount} old records", oldCount);

        // Сохраняем новые терминалы
        await db.Offices.AddRangeAsync(terminals, stoppingToken);
        var newCount = await db.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Saved {NewCount} new terminals", newCount);
    }
}
