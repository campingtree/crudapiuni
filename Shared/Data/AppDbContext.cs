using CrudApp.Models;
using CrudApp.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrudApp.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IOptions<DatabaseOptions> databaseOptions)
    : DbContext(options)
{
    public DbSet<PointOfInterest> Points => Set<PointOfInterest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(databaseOptions.Value.Schema);
        modelBuilder.Entity<PointOfInterest>(point =>
        {
            point.ToTable("points_of_interest");
            point.HasKey(p => p.Id);
            point.Property(p => p.Name).HasMaxLength(160).IsRequired();
            point.Property(p => p.Description).HasMaxLength(2000);
            point.Property(p => p.PhotoBlobName).HasMaxLength(100);
            point.Property(p => p.PhotoContentType).HasMaxLength(50);
        });
    }
}
