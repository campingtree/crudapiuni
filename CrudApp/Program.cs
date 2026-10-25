using Amazon.S3;
using CrudApp.Components;
using CrudApp.Data;
using CrudApp.Options;
using CrudApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorComponents();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Schema), "Database:Schema is required.")
    .ValidateOnStart();
builder.Services.AddOptions<S3StorageOptions>()
    .Bind(builder.Configuration.GetSection("S3Storage"))
    .Validate(o => Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var endpoint) && endpoint.Scheme == Uri.UriSchemeHttps,
        "S3Storage:Endpoint must be an HTTPS URL.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "S3Storage:BucketName is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.AccessKey), "S3Storage:AccessKey is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey), "S3Storage:SecretKey is required.")
    .ValidateOnStart();

var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres));
builder.Services.AddSingleton<IAmazonS3>(services =>
{
    var storage = services.GetRequiredService<IOptions<S3StorageOptions>>().Value;
    return new AmazonS3Client(storage.AccessKey, storage.SecretKey, new AmazonS3Config
    {
        ServiceURL = storage.Endpoint,
        ForcePathStyle = false
    });
});
builder.Services.AddScoped<PointPhotoStorage>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
