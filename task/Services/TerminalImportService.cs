using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TerminalDirectory.Entities;
using TerminalDirectory.Infrastructure;

namespace TerminalDirectory.Services;

public class TerminalImportService : BackgroundService
{
    private readonly ILogger<TerminalImportService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TerminalImportService(ILogger<TerminalImportService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TerminalImportService запуск.");

#if DEBUG
        await ImportTerminalsAsync(CancellationToken.None);
        _logger.LogInformation("Тестовый импорт выполнен сразу при старте");
#endif

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowMsk = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Russian Standard Time");
                var nextRun = nowMsk.Date.AddHours(2); // 02:00 MSK
                if (nowMsk >= nextRun) nextRun = nextRun.AddDays(1);

                var delay = nextRun - nowMsk;
                _logger.LogInformation("Next import scheduled in {Hours}h {Minutes}m", delay.Hours, delay.Minutes);
                await Task.Delay(delay, stoppingToken);

                await ImportTerminalsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("TerminalImportService stopping gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TerminalImportService loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ImportTerminalsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var _dbContext = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "files", "terminals.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("JSON файл {Path} не найден.", jsonPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);

            var offices = JsonSerializer.Deserialize<List<Office>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Office>();

            _logger.LogInformation("Loaded {Count} terminals from JSON", offices.Count);

            var oldCount = await _dbContext.Offices.CountAsync(stoppingToken);
            _dbContext.Offices.RemoveRange(_dbContext.Offices);
            await _dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Deleted {OldCount} old records", oldCount);

            await _dbContext.Offices.AddRangeAsync(offices, stoppingToken);
            await _dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Saved {NewCount} new terminals", offices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта");
        }
    }
}