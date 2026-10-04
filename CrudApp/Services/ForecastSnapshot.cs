namespace CrudApp.Services;

public sealed record ForecastSnapshot(
    DateOnly Date,
    double? MinC,
    double? MaxC,
    int? PrecipitationProbability,
    int? WeatherCode);
