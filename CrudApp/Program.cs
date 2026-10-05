using Azure.Storage.Blobs;
using CrudApp.Components;
using CrudApp.Data;
using CrudApp.Options;
using CrudApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorComponents();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Schema), "Database:Schema is required.")
    .ValidateOnStart();
builder.Services.AddOptions<BlobStorageOptions>()
    .Bind(builder.Configuration.GetSection("BlobStorage"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ContainerName), "BlobStorage:ContainerName is required.")
    .ValidateOnStart();

var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
var blobConnection = builder.Configuration.GetConnectionString("BlobStorage")
    ?? throw new InvalidOperationException("ConnectionStrings:BlobStorage is required.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres));
builder.Services.AddSingleton(new BlobServiceClient(blobConnection));
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
