using CloudWeather.Report.Config;
using CloudWeather.Report.DataAccess;
using CloudWeather.Report.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CloudWeather.Report.BusinessLogic
{
  /// <summary>
  /// Aggregates data from multiple external sources to build a weather report.
  /// </summary>
  public interface IWeatherReportAggregator
  {
    /// <summary>
    /// Builds and returns a Weather Report.
    /// Persists WeatherReport.
    /// </summary>
    /// <param name="zip"></param>
    /// <param name="days"></param>
    /// <returns></returns>
    public Task<WeatherReport> BuildReport(string zip, int days);
  }

  public class WeatherReportAggregator : IWeatherReportAggregator
  {
    private readonly IHttpClientFactory http;
    private readonly ILogger<WeatherReportAggregator> logger;
    private readonly WeatherDataConfig weatherDataConfig;
    private readonly WeatherReportDbContext db;

    private async Task<List<TemperatureModel>> FetchTemperatureData(HttpClient httpCliet, string zip, int days)
    {
      var endpoint = BuildTemperatureServiceEndpoint(zip, days);
      var temperatureRecords = await httpCliet.GetAsync(endpoint);

      var jsonSerializerOptions = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      var temperatureData = await temperatureRecords.Content
        .ReadFromJsonAsync<List<TemperatureModel>>(jsonSerializerOptions);

      return temperatureData ?? new List<TemperatureModel>();
    }

    private async Task<List<PrecipitationModel>> FetchPrecipitationData(HttpClient httpCliet, string zip, int days)
    {
      var endpoint = BuildPrecipitationServiceEndpoint(zip, days);
      var precipRecords = await httpCliet.GetAsync(endpoint);

      var jsonSerializerOptions = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      var precipData = await precipRecords.Content
        .ReadFromJsonAsync<List<PrecipitationModel>>(jsonSerializerOptions);

      return precipData ?? new List<PrecipitationModel>();
    }

    private string BuildTemperatureServiceEndpoint(string zip, int days)
    {
      var tempServiceProtocol = weatherDataConfig.TempDataProtocol;
      var tempServiceHost = weatherDataConfig.TempDataHost;
      var tempServicePort = weatherDataConfig.TempDataPort;

      return $"{tempServiceProtocol}://{tempServiceHost}:{tempServicePort}/observation/{zip}?days={days}";
    }

    private string BuildPrecipitationServiceEndpoint(string zip, int days)
    {
      var precipServiceProtocol = weatherDataConfig.PrecipDataProtocol;
      var precipServiceHost = weatherDataConfig.PrecipDataHost;
      var precipServicePort = weatherDataConfig.PrecipDataPort;

      return $"{precipServiceProtocol}://{precipServiceHost}:{precipServicePort}/observation/{zip}?days={days}";
    }

    private static decimal GetTotalSnow(IEnumerable<PrecipitationModel> precipData)
    {
      var totalSnow = precipData
        .Where(x => x.WeatherType == "snow")
        .Sum(x => x.AmountInches);

      return Math.Round(totalSnow, 1);
    }

    private static decimal GetTotalRain(IEnumerable<PrecipitationModel> precipData)
    {
      var totalSnow = precipData
        .Where(x => x.WeatherType == "rain")
        .Sum(x => x.AmountInches);

      return Math.Round(totalSnow, 1);
    }

    public WeatherReportAggregator(
      IHttpClientFactory http,
      ILogger<WeatherReportAggregator> logger,
      IOptions<WeatherDataConfig> weatherDataConfig,
      WeatherReportDbContext db)
    {
      this.http = http;
      this.logger = logger;
      this.weatherDataConfig = weatherDataConfig.Value;
      this.db = db;
    }

    public async Task<WeatherReport> BuildReport(string zip, int days)
    {
      var httpCliet = http.CreateClient();

      List<PrecipitationModel> precipData = await FetchPrecipitationData(httpCliet, zip, days);
      var totalSnow = GetTotalSnow(precipData);
      var totalRain = GetTotalRain(precipData);

      logger.LogInformation(
        $"zip: {zip} over last {days} days: " +
        $"total snow: {totalSnow}, total rain: {totalRain}"
        );

      List<TemperatureModel> tempData = await FetchTemperatureData(httpCliet, zip, days);
      var averageHighTemp = tempData.Average(t => t.TempHighF);
      var averageLowTemp = tempData.Average(t => t.TempLowF);

      logger.LogInformation(
        $"zip: {zip} over last {days} days: " +
        $"low temp: {averageLowTemp}, high temp: {averageHighTemp}"
        );

      var weatherReport = new WeatherReport
      {
        ZipCode = zip,
        CreatedOn = DateTime.UtcNow,
        AverageHighF = averageHighTemp,
        AverageLowF = averageLowTemp,
        RainfallTotalInches = totalRain,
        SnowTotalInches = totalSnow
      };

      //todo: use 'cached' weather reports instead of making rountrips when possible
      db.Add(weatherReport);
      await db.SaveChangesAsync();

      return weatherReport;
    }
  }
}
