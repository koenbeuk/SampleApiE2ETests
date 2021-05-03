using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleAPI
{
    public class WeatherForecastService
    {
        static readonly string[] _summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        static readonly Random _random = new();

        readonly ConcurrentDictionary<DateTime, WeatherForecast> _database = new();

        WeatherForecast GenerateRandomWeatherForecast(DateTime date)
        {
            return new WeatherForecast
            {
                Date = date,
                TemperatureC = _random.Next(-20, 55),
                Summary = _summaries[_random.Next(_summaries.Length)]
            };
        }

        public WeatherForecast Get(DateTime date)
        {
            return _database.GetOrAdd(date, GenerateRandomWeatherForecast);
        }

        public void Set(DateTime date, WeatherForecast forecast)
        {
            _database[date] = forecast;
        }
    }
}
