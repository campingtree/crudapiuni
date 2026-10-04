using System.ComponentModel.DataAnnotations;

namespace CrudApp.Contracts;

public sealed class PointRequest
{
    [Required, StringLength(160)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(-90, 90)]
    public required double Latitude { get; set; }

    [Range(-180, 180)]
    public required double Longitude { get; set; }

    public DateOnly? VisitedOn { get; set; }
    public bool IsFavorite { get; set; }

    [Range(0, 5)]
    public int Priority { get; set; }
}
