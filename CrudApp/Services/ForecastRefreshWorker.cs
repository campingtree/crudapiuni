using CrudApp.Data;
using CrudApp.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrudApp.Services;

public sealed class ForecastRefreshWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ForecastOptions> options,
    ILogger<ForecastRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.RefreshIntervalMinutes));
        do
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Forecast refresh pass failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var forecastClient = scope.ServiceProvider.GetRequiredService<OpenMeteoForecastClient>();
        var points = await db.Points.AsNoTracking()
            .Select(p => new { p.Id, p.Latitude, p.Longitude })
            .ToListAsync(cancellationToken);

        foreach (var point in points)
        {
            try
            {
                var forecast = await forecastClient.GetTomorrowAsync(point.Latitude, point.Longitude, cancellationToken);
                await db.Points.Where(p => p.Id == point.Id && p.Latitude == point.Latitude && p.Longitude == point.Longitude)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.ForecastDate, forecast.Date)
                        .SetProperty(p => p.ForecastMinC, forecast.MinC)
                        .SetProperty(p => p.ForecastMaxC, forecast.MaxC)
                        .SetProperty(p => p.ForecastPrecipitationProbability, forecast.PrecipitationProbability)
                        .SetProperty(p => p.ForecastWeatherCode, forecast.WeatherCode)
                        .SetProperty(p => p.ForecastUpdatedAtUtc, DateTime.UtcNow), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not refresh forecast for point {PointId}", point.Id);
            }
        }
    }
}
