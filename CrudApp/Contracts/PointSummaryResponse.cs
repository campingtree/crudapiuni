namespace CrudApp.Contracts;

public sealed record PointSummaryResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    bool IsFavorite,
    int Priority,
    ForecastResponse? Forecast);
