using Learning.API.AKS;
using Microsoft.AspNetCore.Mvc;

namespace Learning.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ListNamespaces _listNamespaces;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ListNamespaces listNamespaces)
        {
            _logger = logger;
            _listNamespaces = listNamespaces;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<string> Get()
        {
            return await _listNamespaces.RunAsync().ConfigureAwait(false);
        }
    }
}