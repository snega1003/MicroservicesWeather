using Microsoft.EntityFrameworkCore;

namespace CloudWeather.Report.DataAccess
{
  public class WeatherReportDbContext : DbContext
  {
    private static void SnakeCaseIdentityTableNames(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<WeatherReport>(b => { b.ToTable("temperature"); });
    }

    public WeatherReportDbContext() { }

    public WeatherReportDbContext(DbContextOptions opts) : base(opts) { }

    public DbSet<WeatherReport> WeatherReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      SnakeCaseIdentityTableNames(modelBuilder);
    }
  }
}
