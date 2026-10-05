namespace CrudApp.Models;

public sealed class PointOfInterest
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateOnly? VisitedOn { get; set; }
    public bool IsFavorite { get; set; }
    public int Priority { get; set; }
    public string? PhotoBlobName { get; set; }
    public string? PhotoContentType { get; set; }
    public DateOnly? ForecastDate { get; set; }
    public double? ForecastMinC { get; set; }
    public double? ForecastMaxC { get; set; }
    public int? ForecastPrecipitationProbability { get; set; }
    public int? ForecastWeatherCode { get; set; }
    public DateTime? ForecastUpdatedAtUtc { get; set; }
}
