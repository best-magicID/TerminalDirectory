using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TerminalDirectory.Entities;
using TerminalDirectory.Infrastructure;

namespace TerminalDirectory.Services;

public class TerminalImportService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<TerminalImportService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TerminalImportService(ILogger<TerminalImportService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TerminalImportService started");

#if DEBUG
        await ImportTerminalsAsync(stoppingToken);
        _logger.LogInformation("Initial import completed at startup (DEBUG)");
#endif

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowMsk = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Russian Standard Time");
                var nextRun = nowMsk.Date.AddHours(2);
                if (nowMsk >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - nowMsk;
                _logger.LogInformation("Next import scheduled at {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);
                await ImportTerminalsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("TerminalImportService stopping gracefully");
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
            var dbContext = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "files", "terminals.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("JSON file {Path} not found", jsonPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
            var offices = DeserializeOffices(json);

            _logger.LogInformation("Loaded {Count} terminals from JSON", offices.Count);

            var oldCount = await dbContext.Offices.CountAsync(stoppingToken);
            dbContext.Phones.RemoveRange(dbContext.Phones);
            dbContext.Offices.RemoveRange(dbContext.Offices);
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Deleted {OldCount} old records", oldCount);

            await dbContext.Offices.AddRangeAsync(offices, stoppingToken);
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Saved {NewCount} new terminals", offices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта");
        }
    }

    private List<Office> DeserializeOffices(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        if (json.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            var direct = JsonSerializer.Deserialize<List<Office>>(json, JsonOptions);
            return direct ?? [];
        }

        var source = JsonSerializer.Deserialize<SourceRoot>(json, JsonOptions);
        if (source?.City is null || source.City.Count == 0)
        {
            return [];
        }

        var offices = new List<Office>();
        foreach (var city in source.City)
        {
            if (!city.CityId.HasValue)
            {
                _logger.LogWarning("City '{CityName}' has null cityID and will be skipped", city.Name);
                continue;
            }

            var cityTerminals = city.Terminals?.Terminal;
            if (cityTerminals is null)
            {
                continue;
            }

            foreach (var terminal in cityTerminals)
            {
                var office = new Office
                {
                    Code = terminal.Id,
                    CityCode = city.CityId.Value,
                    Uuid = terminal.Id,
                    Type = ResolveOfficeType(terminal),
                    CountryCode = "RU",
                    Coordinates = new Coordinates
                    {
                        Latitude = ParseDouble(terminal.Latitude),
                        Longitude = ParseDouble(terminal.Longitude)
                    },
                    AddressCity = city.Name,
                    AddressStreet = terminal.Address,
                    WorkTime = terminal.Worktables?.Worktable?.FirstOrDefault()?.Timetable ?? string.Empty,
                    Phones = terminal.Phones?
                        .Where(p => !string.IsNullOrWhiteSpace(p.Number))
                        .Select(p => new Phone
                        {
                            PhoneNumber = p.Number!,
                            Additional = p.Comment
                        })
                        .ToList() ?? []
                };

                offices.Add(office);
            }
        }

        return offices;
    }

    private static OfficeType ResolveOfficeType(SourceTerminal terminal)
    {
        if (terminal.IsPvz)
        {
            return OfficeType.PVZ;
        }

        if (terminal.IsOffice)
        {
            return OfficeType.POSTAMAT;
        }

        return OfficeType.WAREHOUSE;
    }

    private static double ParseDouble(string? value)
    {
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private sealed class SourceRoot
    {
        public List<SourceCity> City { get; set; } = [];
    }

    private sealed class SourceCity
    {
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("cityID")]
        public int? CityId { get; set; }

        public SourceTerminals? Terminals { get; set; }
    }

    private sealed class SourceTerminals
    {
        public List<SourceTerminal> Terminal { get; set; } = [];
    }

    private sealed class SourceTerminal
    {
        public string? Id { get; set; }
        public string? Address { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        [JsonPropertyName("isPVZ")]
        public bool IsPvz { get; set; }

        [JsonPropertyName("isOffice")]
        public bool IsOffice { get; set; }

        public List<SourcePhone>? Phones { get; set; }
        public SourceWorkTables? Worktables { get; set; }
    }

    private sealed class SourcePhone
    {
        public string? Number { get; set; }
        public string? Comment { get; set; }
    }

    private sealed class SourceWorkTables
    {
        public List<SourceWorkTable>? Worktable { get; set; }
    }

    private sealed class SourceWorkTable
    {
        public string? Timetable { get; set; }
    }
}
