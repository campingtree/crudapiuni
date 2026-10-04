namespace CrudApp.Contracts;

public sealed record ForecastResponse(
    DateOnly Date,
    double? MinC,
    double? MaxC,
    int? PrecipitationProbability,
    int? WeatherCode,
    DateTime UpdatedAtUtc);
