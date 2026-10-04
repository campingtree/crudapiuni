using Azure;
using CrudApp.Contracts;
using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrudApp.Controllers;

[ApiController]
[Route("api/points")]
public sealed class PointsController(AppDbContext db, PointPhotoStorage photos, ILogger<PointsController> logger)
    : ControllerBase
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoTypes = ["image/jpeg", "image/png", "image/webp"];

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PointSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PointSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        var points = await db.Points.AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return Ok(points.Select(p => new PointSummaryResponse(p.Id, p.Name, p.Latitude, p.Longitude,
            p.IsFavorite, p.Priority, ToForecast(p))).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PointDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointDetailResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var point = await db.Points.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return point is null ? NotFound() : Ok(ToDetail(point));
    }

    [HttpPost]
    [ProducesResponseType<PointDetailResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PointDetailResponse>> Create(PointRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var point = new PointOfInterest { Id = Guid.NewGuid(), Name = request.Name.Trim() };
        ApplyRequest(point, request);
        db.Points.Add(point);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = point.Id }, ToDetail(point));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PointDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointDetailResponse>> Update(Guid id, PointRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var point = await db.Points.FindAsync([id], cancellationToken);
        if (point is null)
            return NotFound();

        var coordinatesChanged = point.Latitude != request.Latitude || point.Longitude != request.Longitude;
        ApplyRequest(point, request);
        if (coordinatesChanged)
        {
            point.ForecastDate = null;
            point.ForecastMinC = null;
            point.ForecastMaxC = null;
            point.ForecastPrecipitationProbability = null;
            point.ForecastWeatherCode = null;
            point.ForecastUpdatedAtUtc = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDetail(point));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var point = await db.Points.FindAsync([id], cancellationToken);
        if (point is null)
            return NotFound();

        db.Points.Remove(point);
        await db.SaveChangesAsync(cancellationToken);
        await DeleteOldPhotoAsync(point.PhotoBlobName, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPhotoBytes + 1024 * 1024)]
    [ProducesResponseType<PointDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PointDetailResponse>> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var point = await db.Points.FindAsync([id], cancellationToken);
        if (point is null)
            return NotFound();
        if (file.Length == 0 || file.Length > MaxPhotoBytes || !AllowedPhotoTypes.Contains(file.ContentType))
            return BadRequest("Upload one JPEG, PNG, or WebP image up to 5 MB.");

        await using (var validationStream = file.OpenReadStream())
        {
            if (file.Length < 12 || !await HasValidSignatureAsync(validationStream, file.ContentType, cancellationToken))
                return BadRequest("The uploaded file does not match its image type.");
        }

        await using var input = file.OpenReadStream();
        var newBlobName = await photos.UploadAsync(input, file.ContentType, cancellationToken);
        var oldBlobName = point.PhotoBlobName;
        point.PhotoBlobName = newBlobName;
        point.PhotoContentType = file.ContentType;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteOldPhotoAsync(newBlobName, CancellationToken.None);
            throw;
        }

        await DeleteOldPhotoAsync(oldBlobName, cancellationToken);
        return Ok(ToDetail(point));
    }

    [HttpGet("{id:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken cancellationToken)
    {
        var point = await db.Points.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (point?.PhotoBlobName is null)
            return NotFound();

        try
        {
            var download = await photos.DownloadAsync(point.PhotoBlobName, cancellationToken);
            return File(download.Content, point.PhotoContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePhoto(Guid id, CancellationToken cancellationToken)
    {
        var point = await db.Points.FindAsync([id], cancellationToken);
        if (point is null)
            return NotFound();

        var oldBlobName = point.PhotoBlobName;
        point.PhotoBlobName = null;
        point.PhotoContentType = null;
        await db.SaveChangesAsync(cancellationToken);
        await DeleteOldPhotoAsync(oldBlobName, cancellationToken);
        return NoContent();
    }

    private async Task DeleteOldPhotoAsync(string? blobName, CancellationToken cancellationToken)
    {
        if (blobName is null)
            return;
        try
        {
            await photos.DeleteAsync(blobName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not remove photo blob {BlobName}", blobName);
        }
    }

    private static async Task<bool> HasValidSignatureAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await stream.ReadExactlyAsync(header.AsMemory(), cancellationToken);
        return contentType switch
        {
            "image/jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static void ApplyRequest(PointOfInterest point, PointRequest request)
    {
        point.Name = request.Name.Trim();
        point.Description = request.Description?.Trim();
        point.Latitude = request.Latitude;
        point.Longitude = request.Longitude;
        point.VisitedOn = request.VisitedOn;
        point.IsFavorite = request.IsFavorite;
        point.Priority = request.Priority;
    }

    private static PointDetailResponse ToDetail(PointOfInterest point) => new(
        point.Id, point.Name, point.Description, point.Latitude, point.Longitude, point.VisitedOn,
        point.IsFavorite, point.Priority,
        point.PhotoBlobName is null ? null : $"/api/points/{point.Id}/photo",
        ToForecast(point));

    private static ForecastResponse? ToForecast(PointOfInterest point) =>
        point.ForecastDate is { } date && point.ForecastUpdatedAtUtc is { } updatedAt
            ? new ForecastResponse(date, point.ForecastMinC, point.ForecastMaxC,
                point.ForecastPrecipitationProbability, point.ForecastWeatherCode, updatedAt)
            : null;
}
