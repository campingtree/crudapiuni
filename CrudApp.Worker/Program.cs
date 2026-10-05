using CrudApp.Data;
using CrudApp.Options;
using CrudApp.Worker.Options;
using CrudApp.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Schema), "Database:Schema is required.")
    .ValidateOnStart();
builder.Services.AddOptions<ForecastOptions>()
    .Bind(builder.Configuration.GetSection("Forecast"))
    .Validate(o => o.RefreshIntervalMinutes > 0, "Forecast:RefreshIntervalMinutes must be positive.")
    .ValidateOnStart();

var postgres = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(postgres))
    throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres));
builder.Services.AddHttpClient<OpenMeteoForecastClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHostedService<ForecastRefreshWorker>();

await builder.Build().RunAsync();
