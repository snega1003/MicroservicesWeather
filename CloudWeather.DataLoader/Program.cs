using CloudWeather.DataLoader.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

IConfiguration config = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json")
  .AddEnvironmentVariables()
  .Build();

var servicesConfig = config.GetSection("Services");

var tempServiceConfig = servicesConfig.GetSection("Temperature");
var tempServiceHost = tempServiceConfig["Host"];
var tempServicePort = tempServiceConfig["Port"];

var precipServiceConfig = servicesConfig.GetSection("Precipitation");
var precipServiceHost = precipServiceConfig["Host"];
var precipServicePort = precipServiceConfig["Port"];

var zipCodes = new List<string>()
{
  "73026",
  "64812",
  "15283",
  "90373",
  "12350"
};

Console.WriteLine("Starting Data Load...");

var temperatureHttpClient = new HttpClient();
temperatureHttpClient.BaseAddress = new Uri($"http://{tempServiceHost}:{tempServicePort}");

var precipitationHttpClient = new HttpClient();
precipitationHttpClient.BaseAddress = new Uri($"http://{tempServiceHost}:{tempServicePort}");

foreach (var zip in zipCodes)
{
  Console.WriteLine($"Processing Zip Code: {zip}");
  var from = DateTime.Now.AddYears(-2);
  var thru = DateTime.Now;

  for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
  {
    var temps = PostTemp(zip, day, temperatureHttpClient);
    PostPrecip(temps[0], zip, day, precipitationHttpClient);
  }
}

List<int> PostTemp(string zip, DateTime day, HttpClient temperatureHttpClient)
{
  var rand = new Random();
  var t1 = rand.Next(1, 100);
  var t2 = rand.Next(1, 100);
  var hiLoTemps = new List<int> { t1, t2 };
  hiLoTemps.Sort();

  var temperatureObservation = new TemperatureModel
  {
    TempLowF = hiLoTemps[0],
    TempHighF = hiLoTemps[1],
    ZipCode = zip,
    CreatedOn = day
  };

  var tempResult = temperatureHttpClient
    .PostAsJsonAsync("observation", temperatureObservation)
    .Result;

  if (tempResult.IsSuccessStatusCode)
  {
    Console.Write(
      $"Posted Temperature Date: {day:d} " +
      $"Zip: {zip} " +
      $"High Temp: {hiLoTemps[1]} " +
      $"Low Temp: {hiLoTemps[0]}");
  }
  else
  {
    Console.WriteLine(tempResult.ToString());
  }

  return hiLoTemps;
}

void PostPrecip(int lowTemp, string zip, DateTime day, HttpClient precipitationHttpClient)
{
  var rand = new Random();
  var isPrecip = rand.Next(2) < 1;

  PrecipitationModel precipitation;

  if (isPrecip)
  {
    var precipInches = rand.Next(1, 16);
    precipitation = new PrecipitationModel
    {
      AmountInches = precipInches,
      WeatherType = lowTemp < 32 ? "snow" : "rain",
      ZipCode = zip,
      CreatedOn = day
    };
  }
  else
  {
    precipitation = new PrecipitationModel()
    {
      AmountInches = 0,
      WeatherType = "none",
      ZipCode = zip,
      CreatedOn = day
    };
  }

  var precipResponse = precipitationHttpClient
    .PostAsJsonAsync("observation", precipitation)
    .Result;

  if (precipResponse.IsSuccessStatusCode)
  {
    Console.Write(
      $"Posted Precipitation Date: {day:d} " +
      $"Zip: {zip} " +
      $"Type: {precipitation.WeatherType} " +
      $"Amount (in.) {precipitation.AmountInches}");
  }
}