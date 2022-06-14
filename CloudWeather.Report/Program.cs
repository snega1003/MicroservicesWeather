using CloudWeather.Report.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<WeatherReportDbContext>(
  opts =>
  {
    opts.EnableSensitiveDataLogging();
    opts.EnableDetailedErrors();
    opts.UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
  },
  ServiceLifetime.Transient);

// Add services to the container.
var app = builder.Build();

app.Run();
