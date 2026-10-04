namespace CrudApp.Contracts;

public sealed record PointDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    DateOnly? VisitedOn,
    bool IsFavorite,
    int Priority,
    string? PhotoUrl,
    ForecastResponse? Forecast);
